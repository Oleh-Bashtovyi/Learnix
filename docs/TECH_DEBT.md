# Learnix — Technical Debt

> Things that work but are implemented suboptimally. Each entry describes the current state, why it is a problem, and a specific plan for fixing it.
>
> Priorities: `high` · `medium` · `low`

---

## TD-001 · A deleted user cannot actually be deleted — `Payments` cascades, and the FK graph is inconsistent

**Priority:** `high` (data integrity + a privacy promise we only half keep)

**Current state.** `DeletedAccountPurgeService` runs 24 h after `User.PurgeAfter` expires and **anonymizes** the account (ADR-BACK-USERS-001): the `AspNetUsers` row survives, stripped of email, name, bio, avatar, Google link and password. It cannot hard-delete the row, because the schema does not allow it. The three reasons, from the live database:

| Table → `AspNetUsers` | Rule | What a hard `DELETE` would do |
|---|---|---|
| `CourseReviews.StudentId` | `RESTRICT` | **Fails.** Anyone who ever reviewed a course is undeletable. |
| `CourseConversations.StudentId` / `.InstructorId` | `RESTRICT` | **Fails.** Same for anyone who ever opened a thread. |
| `CourseMessages.SenderId` | `RESTRICT` | **Fails.** Same for a single message. |
| `InstructorApplications.ReviewedByAdminId` | `RESTRICT` | **Fails** for any admin who ever reviewed an application. |
| `Payments.UserId` | **`CASCADE`** | **Destroys the payment history** — the rows instructor earnings and the admin ledger are built from. |
| `Enrollments`, `Certificates`, `LessonProgress`, `TestAttempts`, `Courses` | **no FK at all** | **Silently orphans them.** A certificate whose public verification page can no longer name its holder; a course whose instructor does not exist while its students are still enrolled. |

**Why it is a problem.**
1. **`Payments` cascading from a user is wrong on its own**, purge or no purge. A financial record must outlive the account it was made from. Today, any code path that ever hard-deletes a user silently erases money history.
2. **Five tables reference users with no foreign key.** The database cannot protect them, and nothing tells us when they are orphaned. This is the gap that makes "just delete the row" look safe.
3. The deletion email promises the personal data is erased. Anonymization keeps that promise for the `AspNetUsers` row — but only because everything else was deliberately left in place. That is a defensible policy, not an accident, and it needs to stay a conscious decision rather than a consequence of the schema.

**Plan.**
1. **`Payments.UserId` → `RESTRICT`**, and treat a payment as an immutable financial record that survives its user (it already carries its own amount, course and enrollment ids). Migration + backfill nothing; only the FK rule changes.
2. **Add the missing foreign keys** on `Enrollments.StudentId`, `Certificates.StudentId`, `LessonProgress.StudentId`, `TestAttempts.StudentId`, `Courses.InstructorId` — as `RESTRICT`, so the database states the truth: these records depend on a user who must exist.
3. Only then revisit whether a true hard delete is worth having at all. With (1) and (2) in place the honest answer is likely **no** — an instructor's courses and a student's certificates are not theirs alone to erase — and anonymization stays the terminal state, by design rather than by constraint.

**Where the reasoning lives:** the full account is in the class comment on `DeletedAccountPurgeService`, so anybody who opens the purge job to "fix" it reads why it is written that way before changing it.

---

## TD-002 · The recovery window is enforced, the retention promise is not audited

**Priority:** `low`

**Current state.** `User.PurgeAfter` is written at deletion time from `UserConstants.AccountRecoveryWindowDays` (30) and the purge service honours it. Nothing verifies afterwards that the data really is gone — there is no report, no metric, and no test that walks a user's tables looking for leftovers.

**Plan.** Once TD-001 lands (and the FK graph actually describes reality), add an integration test that soft-deletes a user with reviews, messages, payments, a certificate and an avatar, runs the purge, and asserts exactly which rows survive and in what shape. That test is the specification of the retention policy; the prose in the email is only its summary.

---

## TD-003 · In-app notifications are English-only, while every email is localized — RESOLVED

**Priority:** ~~`medium`~~ · **Resolved** by ADR-NOTIF-001: notifications now store `Type` + `Parameters` (jsonb) and the client renders them through react-i18next. The entry is kept for the record of why.

**Current state.** Emails go through `IStringLocalizer` + `EmailStrings.resx` / `EmailStrings.uk.resx` and are rendered in the recipient's `User.Language` (ADR-EMAIL-002). In-app notifications are not: their title and body are hardcoded English strings — `"Achievement Unlocked"`, `"Certificate Issued"`, `"Your instructor application has been approved. Welcome aboard!"` — written straight into the outbox handlers (`Outbox/Handlers/Notifications/NotificationHandlers.cs`, previously buried in the processor's switch) and stored, already rendered, in the `Notifications` table.

**Why it is a problem.** A Ukrainian user gets a localized email and an English bell notification about the same event. And because the text is stored rendered, switching the interface language later does not re-render what is already in the bell.

**Plan.** Store the notification as *data*, not prose: `Type` (already there) plus a small JSON `Params` blob (`{ "courseTitle": "..." }`, `{ "achievementCode": "..." }`). The client renders it through `react-i18next` from the type and the params, exactly as it renders everything else. The handlers then only decide *what happened*, never in which language to say it — and old rows re-render correctly when the user switches language.
