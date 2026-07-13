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

**Status:** Accepted

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
