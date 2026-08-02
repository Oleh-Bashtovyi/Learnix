# Learnix — ADR: LMS Core

> Covers how a course's content is stored and how a student's progress through it is tracked.

The **domain model** of a course — `Course` as the aggregate root for structure, the publish invariants,
and the test value objects in JSONB — is not here. It is platform-level and lives in
[`platform/DOMAIN.md`](../platform/DOMAIN.md) (ADR-BACK-DOMAIN-004, -006, -007, -008).

`ADR-BACK-LMS-001` (Course as aggregate root) and `ADR-BACK-LMS-003` (test value objects as JSONB) were
**removed** during the audit: they recorded the same two decisions a second time, and the copies had
already drifted — LMS-003 still claimed MongoDB stores reviews, which it never has. One decision, one
record. Numbers are not reused, hence the gaps.

---

## ADR-BACK-LMS-002: Table Per Hierarchy for lesson types

**Context:** a course's curriculum is always rendered whole — every section with every lesson — and the
lessons come in three different shapes: video, post, test.

**Decision:** `VideoLesson`, `PostLesson` and `TestLesson` derive from the abstract `Lesson` and share one
table, `Lessons`, discriminated by the `LessonType` column
(`builder.HasDiscriminator(l => l.LessonType)`). Type-specific columns are simply null for the other types.

**Why:**
- **The course structure is always read whole.** Rendering a curriculum means loading every section with
  every lesson. TPH makes that one join into one table, whatever the mix of types.
- **Most of a lesson is not type-specific.** `SectionId`, `Title`, `DisplayOrder`, `IsHidden`,
  `LessonType` are common; only `VideoBlobPath` (video) and the questions JSONB (test) are not.
- **Order is a property of the section, not of the type.** A single table keeps `DisplayOrder` meaningful
  across a mixed list of lessons; three unrelated tables would not.

**Alternatives:**
- **Table Per Type** — base row in `Lessons`, specifics in `VideoLessons` / `TestLessons`. Correct on
  paper, and it turns the one query that matters (load the curriculum) into a join per subtype.
- **Three unrelated entities, no inheritance** — nothing left to sort a section by.

**Consequences:**
- Adding a lesson type means adding columns to `Lessons` that are null for every other type. That is the
  bill for TPH, and it stays small as long as type-specific state does (a video is a path; a test is one
  JSONB column).

---

## ADR-BACK-LMS-004: Course completion is computed, and it is not driven by domain events

**Context:** the platform needs to know when a student has finished a course, and that follows from many
small events — finishing a lesson, submitting a test — rather than being an action a student takes
directly.

**Decision:** progress is a `LessonProgress` row per (student, lesson), carrying an `IsCompleted` flag.
Course completion is **not** a user action: `ICourseCompletionService.TryCompleteAsync` decides it, and it
is called **directly** from the handlers that can cause it — `MarkLessonComplete` and
`SubmitTestAttempt`. When every visible lesson of the course is done, it calls `enrollment.MarkCompleted()`
**and issues the certificate** (`Certificate.Issue(enrollment, course)`) in the same transaction.

**Why it is a direct call and not a domain event:** domain events are dispatched from
`SavingChangesAsync`, *before* the row is written (ADR-BACK-INFRA-015). A handler reacting to
`LessonCompletedDomainEvent` would query for the very progress row that has not been inserted yet and
conclude the course is unfinished. The direct call sidesteps this by passing
`justCompletedLessonId` — the completion check treats that lesson as done even though the database does
not know it yet. This is a real constraint of the event pipeline, not a shortcut: the same trap is why
outbox handlers cannot query for the change that raised them.

**Why the flag and not the row's existence:**
- A `LessonProgress` row also tracks `LastAccessedAt` — it exists as soon as a student opens a lesson, not
  only when they finish one. Completion is `IsCompleted`, not `EXISTS`.
- Idempotency comes from two guards: `LessonProgress.MarkCompleted()` returns early if already completed,
  and the handler only runs the completion check when `wasAlreadyCompleted` was false. A retried request
  cannot re-issue a certificate.

**Why per-lesson rows rather than a JSONB array on `Enrollment`:**
- "How many students finished this lesson" is a question about a lesson, and a row per (student, lesson)
  answers it with an index. A JSONB array on the enrollment answers it by scanning every enrollment.
- An instructor adding a lesson to a live course must not corrupt anyone's progress: with rows, 10/10
  silently becomes 10/11 and the student simply has one lesson left. Nothing migrates.

**Consequences:**
- Only **visible** lessons count (`GetVisibleLessonCompletionAsync`). Hiding a lesson can therefore complete
  a course for students who had everything else done — which is the intended reading of "hidden means not
  part of the course".
- A `TestLesson` that has questions cannot be completed through `MarkLessonComplete` at all: the handler
  rejects it, because the only honest way to finish a test is to submit it.
- Two handlers can complete a course, so both call the same service. A third one that ever can must call it
  too — this is a convention, not something the compiler enforces.

---

## ADR-BACK-LMS-005: What a student sees after a test is the instructor's decision, and it is one decision

**Context.** `TestAttempt` has always persisted the student's answers — `StudentAnswer(QuestionOrder, SelectedOptionOrders, TextValue)`, in a JSON column. Nothing read them back. `GetMyTestAttempts` returned a score and a date; the only projection of the answers that existed was `GetTestReviewForAi`, so the AI tutor could replay a student's attempt while the student could not.

Meanwhile `SubmitTestAttemptResponse` disclosed everything, unconditionally: `IsCorrect`, `CorrectOptionOrders`, `CorrectTextAnswer`, on every test, for every instructor. A test whose answers must stay unseen — a graded assessment, a certification quiz, a test with a retake limit — could not be built on this platform.

**Decision.** `TestLesson.ReviewMode` (`TestReviewMode`), chosen by the instructor per test:

| Mode | Score | Their answers | Which were wrong | The right answer |
|---|---|---|---|---|
| `ScoreOnly` | ✓ | | | |
| `AnswersOnly` | ✓ | ✓ | | |
| `AnswersAndCorrectness` | ✓ | ✓ | ✓ | |
| `FullReview` *(default)* | ✓ | ✓ | ✓ | ✓ |

Two things make it work, and neither is negotiable:

1. **It is a ladder, not a set of flags.** Each mode discloses everything the one below it does, plus one thing more, so the gates read as `mode >= TestReviewMode.AnswersAndCorrectness`. Independent booleans would make "show the correct answers but not which questions were wrong" representable — a state nobody wants and every caller would have to handle.

2. **It gates every path that can reveal an attempt, from one place.** `TestReviewPolicy` is consulted by the submission response, by `GetTestAttemptReview` (the student's own review of a past attempt — the reason the persisted answers finally have a reader), and by `GetTestReviewForAi`. Three call sites each interpreting the enum for themselves is three chances to disagree, and a disagreement here is a leak.

**Consequences.**
- Gating the *review* alone would have been theatre. The student sees the answers on the results screen the instant they submit; a restriction that leaves that screen untouched restricts nothing but their memory. This is why the submission response obeys the mode too, and why a restrictive mode genuinely changes what the platform does at submission time.
- The AI tutor's charter changed with it. ADR-BACK-CHAT-013 justified `get_my_test_review` on the grounds that the platform had already shown the student everything — true then, false now. It is gated by the same policy, and refuses outright on `ScoreOnly`.
- `ScoreOnly` is `0`, which is also `default(TestReviewMode)`. That is deliberate — the strictest value is the one you get by accident — but it means two things must hold: the API request marks `ReviewMode` `[property: JsonRequired]` (a client that forgets the field is rejected, not silently made strict), and the EF property carries **no** `HasDefaultValue`, or EF would mistake a genuine `ScoreOnly` for "unset" and substitute the database default, quietly turning the strictest mode into the most permissive one.
- Existing tests are backfilled to `FullReview` by the migration. That is not a cautious default: it is what those tests have been doing since the platform was built.

**Rejected alternatives:**
- *An enum plus a `ShowQuestionsAndAnswers` boolean.* The boolean is `mode >= AnswersOnly`. Storing it separately means storing the same fact twice, and the two can then disagree.
- *Three modes, dropping `ScoreOnly`.* It is the cheapest of the four to implement — it is the absence of a review — and it is the only one that supports a genuinely closed assessment.
- *Per-attempt or per-course review policy.* A test is the unit an instructor actually reasons about. A course-wide setting cannot express "the practice quizzes are open, the final is not".

---

## ADR-BACK-LMS-006: A test's questions belong to a version, and an attempt is pinned to the one it was served

**Context:** a student's answer identifies its question and options by position within the test, not by a
stable id, so editing a test's questions after someone has already answered them changes what their
answer is being graded against.

**Decision:** questions move off `TestLesson` and onto a new `TestVersion` entity, one row per edition.
`TestLesson.CurrentVersionId` is the version a new attempt is served; `TestAttempt.TestVersionId` pins the
version an attempt was served at `StartTestAttempt` and never moves. Scoring, review and the AI tutor all
read the attempt's version, never the lesson's current one.

An edit reuses the current version's row while nothing has attempted it yet; the moment something has —
including an attempt still in progress — the edit branches into a new version instead of overwriting it.

**Why:**
- `StudentAnswer` identifies its question and options by **position**, not by a stable id. Any edit that
  changed the question list — reordering, inserting, deleting — silently changed what an already-submitted
  answer was being marked against, while the attempt's stored score stayed frozen and correct. Versioning
  removes the shared mutable state that made this possible, rather than trying to keep positions stable.
- Copy-on-write keeps the common case cheap: a test with no attempts can be edited any number of times and
  still occupies one row. A new row is created only when an edit would otherwise disturb an attempt that
  already exists — submitted or in progress.
- Position (`Order`) stays the answer's key. It only ever needs to be stable *within* a version, which
  copy-on-write already guarantees — giving `Question` a persisted id and migrating every stored answer to
  it would solve the same problem with more moving parts.

**Consequences:**
- `TestLesson.Score`/`MaxScore` moved to `TestVersion`, which is what actually owns the questions.
- The FK from an attempt to its version uses `ON DELETE NO ACTION`, not `RESTRICT` — deleting a lesson
  cascades through both tables in one statement, which `RESTRICT`'s immediate check would otherwise reject
  partway through.
- The lesson's pointer to its current version carries no FK of its own, to avoid a circular reference
  between the two tables; the version's own FK back to the lesson is what keeps that pointer from dangling.
- Versions must be inserted through the repository's `Add`, not `AddAsync` — the latter commits
  immediately, before the lesson row it points at exists.
- A narrow window remains where a new attempt can start while a version overwrite is committing and end up
  pinned to a version that changes under it. Closing it needs row-level locking the repository layer
  doesn't expose yet; until then its worst case is the old bug, for one attempt, once.

**Rejected alternatives:**
- *A full snapshot of the questions on every attempt.* Simpler — no version table, no branch/overwrite
  decision — but it stores a copy per *attempt* where versioning stores one per *edit after an attempt*,
  and edits are far rarer than attempts.
- *A stable `Question.Id` with incremental updates.* Fixes reordering, but a deleted question or a changed
  answer key still corrupts an existing attempt — it manages the problem rather than removing it.
- *Refusing to edit a test once it has attempts.* Safe, but makes fixing a typo permanent the moment one
  student has taken the test.

---

## ADR-BACK-LMS-007: Instructor test-performance stats count only the current version's attempts

**Context:** since a test can carry multiple `TestVersion`s (ADR-BACK-LMS-006), a test's attempts do not
all share the same `MaxScore` once it has been edited after being attempted. The instructor-analytics
aggregation grouped attempts by `(CourseId, TestLessonId)` alone, averaging `Score` across whatever
versions existed and reporting `Max(MaxScore)` as *the* max — a number with no consistent denominator the
moment a test had ever been re-edited.

**Decision:** `GetPerformanceByTestAsync` joins each attempt to its lesson and keeps only attempts where
`TestAttempt.TestVersionId == TestLesson.CurrentVersionId`. The response DTO also carries `AttemptsCount`.

**Why:**
- An instructor asking "how is this test doing" means the test as it exists today, not a blend that
  includes questions that no longer exist.
- Every attempt left in a bucket now shares one `MaxScore` by construction, so exposing it alongside the
  average is meaningful again.
- `AttemptsCount` matters because a pass rate reads differently at 2 attempts than at 200; without it the
  number invites a confidence it hasn't earned.

**Rejected alternatives:**
- *A row per `TestVersion`.* Most accurate, but a test edited five times produces five rows for the same
  lesson — the table stops being a one-glance summary, which is what this endpoint is for.
- *Normalize each attempt's raw score to a percentage before averaging, keep every version.* Removes the
  mismatched-denominator problem but still blends a test's easy first draft with its harder current
  edition into one number that describes neither.

**Consequences:**
- Attempts against a superseded version are invisible to this endpoint. They are not lost — `TestAttempt`
  rows are untouched — just excluded from what "how is this test performing" answers.
- A course-scoped instructor analytics query (`GetInstructorTestPerformanceQuery(CourseId)`) follows the
  same not-owner-is-forbidden pattern as `GetInstructorRatingDistributionQuery`.

---

## ADR-BACK-LMS-008: Test-performance stats load curriculum only for the courses that produced a result

**Context:** resolving a test lesson's title (for the response DTO) needs that lesson's course loaded with
its sections, but the handler was loading every course the instructor owns — sections and lessons
included — regardless of how many of those courses actually had a bucket to report on.

**Decision:** `GetInstructorTestPerformanceQueryHandler` first loads the instructor's owned course ids with
no `Include` at all (only needed for the ownership check and to build the id list `GetPerformanceByTestAsync`
queries against). Once the buckets come back, it loads sections/lessons only for the distinct course ids
that actually appear in a bucket — and skips that second query entirely when there are no buckets.

**Why:**
- An instructor with many courses but only a handful of tests attempted was paying, on every visit, for
  the full section/lesson tree of courses that had nothing to do with the response.
- The frontend already narrows this endpoint to one course at a time (no "all courses" mode — see the
  `useInstructorTestPerformance` hook), so in practice this second query now loads exactly one course's
  curriculum, not the instructor's entire catalog.

**Consequences:**
- `InstructorCoursesForAnalyticsSpecification` gained an optional `courseIds` filter, reusable by any
  other analytics handler that needs sections for a known subset of courses rather than every owned one.
