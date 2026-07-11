# Learnix — ADR: Users, Deletion and Retention

> Covers the lifecycle of an account: ban, soft delete, recovery, and what happens when the recovery window runs out.

---

## ADR-USERS-001: Deleting an Account — Soft Delete, a Promised Date, and Anonymization at the End

**Decision:** Deleting a user is a three-stage lifecycle, and it never ends in a `DELETE` of the row.

1. **Soft delete.** `AdminDeleteUser` calls `User.SoftDelete()`: `IsDeleted = true`, `DeletedAt = now`, and `PurgeAfter = now + UserConstants.AccountRecoveryWindowDays` (30). `User` is `ISoftDeletable`, so the global query filter hides them from every query at once — including Identity's, which is why a deleted user's login attempt reports *no such account* rather than *account suspended*, the way a ban does.
2. **The window.** `UserDeletedDomainEvent(UserId, PurgeAfterUtc)` enqueues the deletion email, which names **the date** — not "30 days". An admin can `Recover()` any time before it, which clears `DeletedAt` and `PurgeAfter` and emails the user that the account is back.
3. **The purge.** `DeletedAccountPurgeService` (daily) takes accounts whose `PurgeAfter` has passed, deletes everything that is purely theirs — refresh tokens, notifications, wishlist, achievements, Identity logins/tokens/claims, and their AI chat sessions in Mongo — and calls `User.Anonymize()`: a dead `deleted-{id}@learnix.invalid` address, "Deleted user", no bio, no avatar, no Google link, no password, a fresh security stamp. `PurgeAfter` is cleared so nothing is purged twice.

**Why the date is a column and not a calculation:**
The deletion email tells a person the day their data dies. Recomputing that day from a constant means changing the constant retroactively moves a date somebody was already promised. `PurgeAfter` is written once, at deletion, and is the single source of truth for the email, the admin UI and the purge job alike. The constant only decides what the *next* deletion is promised.

It travels **on the domain event** as well, because domain events are dispatched before the `UPDATE` runs (ADR-BACK-ARCH-008): a handler that went looking for `PurgeAfter` in the database would read `null`.

**Why anonymization rather than deletion:**
The schema forbids the alternative, in three different ways at once — reviews, conversations, messages and admin-reviewed applications are `ON DELETE RESTRICT` (the delete would *fail*); `Payments` is `ON DELETE CASCADE` (the delete would *erase financial history*); and enrollments, certificates, progress, attempts and courses carry the user's id with **no foreign key at all** (the delete would *silently orphan them*). Beyond the schema, the deeper reason: a review is part of a course's rating, a message is half of somebody else's thread, an instructor's course has living students in it. Leaving the platform is not a licence to rewrite what other people see.

The full table of constraints, and the plan to fix the two that are outright bugs (cascading payments, missing FKs), is **TD-001** in `docs/TECH_DEBT.md`, and the reasoning is repeated in the class comment on `DeletedAccountPurgeService` so it is read by whoever opens the job intending to "just delete the row".

**Ban is a different thing entirely, and is left alone:** a ban sets `LockoutEnd` and nothing else. The account stays visible, its author name stays on its reviews, and the user is told they are suspended. Ban is moderation; deletion is absence.

**Rejected alternatives:**
- *Hard delete with cascades.* Deletes other people's data — reviews their courses are rated by, threads their instructor is half of — and takes the payment history with it.
- *Hard delete, blocked for users who have any of those records.* The rule "you can only erase your account if you never wrote anything" is one nobody can act on, and it makes the retention promise depend on the user's history rather than on policy.
- *Computing the purge date from the constant at purge time.* Cheaper, and quietly breaks the promise the moment the constant changes.
- *Purging on a schedule per user (a job row per deletion).* An indexed `PurgeAfter` column and one daily sweep does the same work without a second source of truth to keep in step.

**Consequences:**
- Reviews and messages of a purged user survive, attributed to "Deleted user". Course ratings do not move when somebody leaves.
- Certificates of a purged user remain verifiable; the public verification page already tolerates a missing user (`"Unknown"`).
- The `AspNetUsers` row is never reused: the placeholder address is in the `.invalid` TLD (RFC 6761), so it can never collide with a real one a returning user registers.
- A partial index on `PurgeAfter` (`WHERE PurgeAfter IS NOT NULL`) keeps the daily sweep off the live users.
