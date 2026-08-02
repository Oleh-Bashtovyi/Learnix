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

**Priority:** ~~`medium`~~ · **Resolved** by ADR-BACK-NOTIF-001: notifications now store `Type` + `Parameters` (jsonb) and the client renders them through react-i18next. The entry is kept for the record of why.

**Current state.** Emails go through `IStringLocalizer` + `EmailStrings.resx` / `EmailStrings.uk.resx` and are rendered in the recipient's `User.Language` (ADR-BACK-EMAIL-002). In-app notifications are not: their title and body are hardcoded English strings — `"Achievement Unlocked"`, `"Certificate Issued"`, `"Your instructor application has been approved. Welcome aboard!"` — written straight into the outbox handlers (`Outbox/Handlers/Notifications/NotificationHandlers.cs`, previously buried in the processor's switch) and stored, already rendered, in the `Notifications` table.

**Why it is a problem.** A Ukrainian user gets a localized email and an English bell notification about the same event. And because the text is stored rendered, switching the interface language later does not re-render what is already in the bell.

**Plan.** Store the notification as *data*, not prose: `Type` (already there) plus a small JSON `Params` blob (`{ "courseTitle": "..." }`, `{ "achievementCode": "..." }`). The client renders it through `react-i18next` from the type and the params, exactly as it renders everything else. The handlers then only decide *what happened*, never in which language to say it — and old rows re-render correctly when the user switches language.

---

## TD-004 · Shared course links show a generic preview — non-JS scrapers never see the page's own OG tags

**Priority:** `medium` (every course link posted to a social network looks like the landing page)

**Current state.** The client is a Vite SPA with no SSR. Per-page metadata is rendered by React through `<Seo />` (ADR-FRONT-INTL-002), which means it only exists *after* JavaScript runs. Googlebot executes JS and sees it. Facebook, LinkedIn, Slack, Telegram and Twitter do not — they read the raw `index.html`, whose fallback tags describe the landing page. So a shared `/courses/{id}` link never shows the course's title, description or cover image, and the `Course` JSON-LD is invisible to anything that doesn't run scripts.

**Why it is a problem.** Course links are the ones people actually share. The `og:image` we generate and the per-course tags we build are, for the single most common sharing path, dead code.

**Plan.** Two options, in increasing order of cost:
1. **Prerender the static public routes** (`/`, `/courses`, `/faq`, `/about`, `/become-instructor`) at build time with a headless-browser prerender plugin. Cheap, but does nothing for `/courses/{id}` — the pages that matter.
2. **Inject metadata at the edge** for `/courses/{id}` and `/instructors/{id}`: a small function in front of the static host that detects a bot user-agent, fetches the course from the API and rewrites the `<head>` of `index.html` before serving it. Azure Static Web Apps supports managed functions; the alternative is moving the frontend behind a Node/edge host, which is the same migration cost as adopting SSR outright.

Until one of them lands, the `index.html` fallback tags are the *only* thing scrapers ever see — keep them accurate.

---

## TD-005 · The email logo is an inline CID attachment, and Gmail renders it as neither

**Priority:** `medium` (every transactional email looks broken, and the brand is the first thing the reader sees)

**Current state.** `SmtpEmailSender` attaches `Email/Resources/logo.png` as a MailKit `LinkedResource` with `ContentId = learnix-logo`, and `_Layout.cshtml` references it as `<img src="cid:learnix-logo">`. The MIME this produces is correct — verified byte by byte against a locally delivered message: `multipart/alternative` → `text/plain` + `multipart/related` (the HTML plus an `image/png` part carrying `Content-Disposition: inline` and `Content-Id: <learnix-logo>`). Mail clients that honour it show the logo in the header.

Gmail does not. It leaves an empty box where the logo belongs and lists the image at the bottom as a file attachment. This is **not** the spam folder blocking images — it persists now that delivery reaches the inbox.

**Why it is a problem.** Beyond the broken header: a `cid` part is a real attachment on the wire, so every email carries the logo's bytes, and clients that don't resolve the `cid` show the reader a paperclip on a message that has nothing to download.

**Root cause: unconfirmed.** The message we generate is right, so something between us and the reader is not: the most likely candidate is the SMTP relay rewriting the MIME (several providers flatten `multipart/related` or drop `Content-ID`, which turns a linked resource into a plain attachment). Diagnosing it needs the *delivered* source — Gmail's "Show original" — not the message we send.

**Plan.** Do what transactional senders actually do and stop embedding the image: host the logo as a static asset on the frontend (it is already a public HTTPS origin — `App:ClientBaseUrl`) and reference it with an absolute URL, e.g. `<img src="@Model.ClientBaseUrl/email-logo.png">`. Then drop the `LinkedResource` entirely.

- **Why this is the standard.** Stripe, GitHub, Postmark, Mailchimp and every provider template do it this way. The message stays small, carries no attachments, and Gmail proxies and caches the image through `googleusercontent.com` — no `cid` resolution to get wrong, and no relay left to mangle it.
- **The trade-off, stated honestly.** The logo becomes an external image, so a client configured to block remote content shows nothing until the reader allows it — where a `cid` image would have rendered. That is the price the whole industry pays, and Gmail loads proxied images by default.
- **Do not skip the diagnosis.** Even after moving to a URL, the raw delivered message is worth reading once: if the relay is rewriting MIME, that is worth knowing before it silently breaks something else.

---

## TD-010 · High code duplication reported by jscpd in C# Unit Tests

**Priority:** `low` (tooling configuration / testing philosophy)

**Current state.** `jscpd` reports a high duplication rate (over 23%) in the C# codebase, which causes the pre-commit hooks to fail. The vast majority of these "clones" are identical Arrange-Act-Assert blocks in `Learnix.Application.UnitTests`, particularly for cross-cutting concerns like testing `WhenUserIsNotAuthenticated` or `WhenUserIsNotAdmin` across multiple command and query handlers.

**Why it is a problem.** While DAMP (Descriptive and Meaningful Phrases) is often preferred over DRY (Don't Repeat Yourself) in unit tests to keep them isolated and readable, this much boilerplate triggers static analysis tools and obscures actual, problematic duplications in the production code. Currently, the entire `**/*UnitTests*/**` pattern has been added to `.jscpdignore` to allow commits to pass.

**Plan.** Decide on a testing philosophy and implement it:
1. **Option A (Keep DAMP):** Decide that test duplication is acceptable for readability. Keep unit tests ignored in `jscpd` permanently and close this issue.
2. **Option B (Refactor to DRY):** Extract common Arrange/Assert logic into shared base classes or helper methods (e.g., `AssertRequiresAuthentication(handler, command)`), which reduces boilerplate but might make tests harder to read top-to-bottom. If implemented, remove the ignore rule from `.jscpd.json`.

---

## TD-011 · Releasing a blob on delete depends on the delete path loading the entity that owns it

**Priority:** `low` (storage hygiene)

**Current state.** `PrepareForDeletionInterceptor` sweeps the EF Core `ChangeTracker` before every save and asks each hard-deleted entity to release the blobs it owns (`BaseEntity.PrepareForDeletion`), which is how a deleted `Category` gives up its image and a deleted `VideoLesson` its video. EF cascades to *loaded* children, so a cascade reaches the sweep too: `DeleteSectionCommandHandler` loads every lesson of the course, and the videos of a deleted section are released along with it. Every hard-delete path in the codebase currently loads the blob-owning entities it destroys, so no path orphans a blob today.

**Why it is a problem.** Nothing enforces that. The sweep only sees what EF tracks, so a future delete path that leaves blob-owning children unloaded — or a specification that stops including lessons because a handler no longer needs them — hands the deletion to the database's `ON DELETE CASCADE`, which takes the rows out without EF ever knowing the entities existed. The files then stay in Azure Storage forever, and nothing in the code shows it: the delete succeeds and the bytes are simply paid for. The safety of this depends on a property of every *caller*, which is exactly the kind of thing that quietly stops being true.

**Plan.** Add a reconciliation job: list each container, diff it against the blob paths still referenced in PostgreSQL, delete what nothing points at. It makes correctness a property of the system rather than of every delete path, and it also collects the blobs orphaned by uploads that were committed but whose entity save then failed — a source of orphans the interceptor cannot see at all.

**Note.** Soft-deletable entities (`Course`, `User`) are deliberately excluded from the sweep: their rows survive and can be recovered, so their blobs have to survive with them. Adding a blob-releasing `OnPreparingForDeletion` override to either of them would be a bug, not a fix.

---

## TD-013 · A blob committed to its final container is never rolled back when the save that follows fails

**Priority:** `low` (storage hygiene — a few cents, and only when a retry is abandoned)

**Current state.** The seven handlers that accept an upload all run the same shape: `CommitUploadAsync` copies the blob out of `temp-uploads` into its final container, the entity is mutated with the returned path, and `SaveChangesAsync` follows. Nothing compensates if that save fails. Azure and PostgreSQL share no transaction, so the copy has already happened and cannot be rolled back with it — the file sits in `avatars/`, `course-videos/` or `category-images/` with no row referencing it. The lifecycle policy will not reap it: it only covers `temp-uploads`, and it must, because there an old blob means "abandoned", while in a final container an old blob usually means a lesson someone still watches.

**Why it is a small problem, not a big one.** The commit is idempotent (ADR-BACK-BLOB-003): the destination keeps the temp blob's name, so a retry copies to the same path and saves the same value — the would-be orphan simply becomes the live file. The temp blob is also left in place, so the retry costs the user nothing, not even a re-upload of a 2 GB video. The orphan survives only when the save fails **and** the user never retries. It then costs roughly four cents a month for a 2 GB video, and nothing notices.

**Plan.** Add a `DeleteAsync` of the committed blob when the save fails, so the abandoned-retry case stops leaking:

```csharp
var commit = await blobStorage.CommitUploadAsync(request.VideoBlobPath, UploadTarget.LessonVideo, cancellationToken);
try
{
    // build the entity, mutate the aggregate, SaveChangesAsync
}
catch
{
    await blobStorage.DeleteAsync(commit.Value.BlobPath, CancellationToken.None);
    throw;
}
```

The awkward part, and the reason this is not done yet: `SaveChangesAsync` is called by the handler, not by `IBlobStorageService`, so the compensation cannot live in one place — it is the same seven-line block repeated in seven handlers, guarding a failure that costs pennies. Before writing it seven times, look for a shape that keeps it in one: a scoped tracker of blobs committed during the request, drained by a pipeline behavior when the request fails, would do it without touching any handler. Do not build the tracker speculatively either — the entry exists so the choice is deliberate rather than forgotten.

**Rejected for now: a `blob_gc` table.** Insert the path before the copy, delete the row in the same transaction as the save, let a worker reap rows that outlive a grace period. This is what a system at scale does, and unlike Approach 3 in ADR-BACK-BLOB-003 it never lists a container — it reads a short candidate list out of its own database, and a grace period removes the race. It is also the only option that survives the process dying between the copy and the `catch`. Not worth its machinery at this size.

**Note.** Whatever is chosen, `catch` must not swallow: the client still needs the failure. And the delete must run on `CancellationToken.None` — the token that just cancelled the save would cancel the cleanup too.
