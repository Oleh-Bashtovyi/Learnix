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
