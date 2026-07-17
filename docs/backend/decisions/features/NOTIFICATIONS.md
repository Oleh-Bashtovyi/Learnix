# Learnix — ADR: In-App Notifications

> The bell: what the server stores, what it pushes, and who chooses the words.

---

## ADR-BACK-NOTIF-001: A Notification Is Data, Not a Sentence

**Decision:** A notification carries **what happened** and **what it happened to** — never prose. `Notification` stores `Type` (the enum) and `Parameters` (a `jsonb` map of strings: `{"courseTitle": "React"}`, `{"code": "FIRST_LESSON"}`), and nothing else. `INotificationSender.SendAsync(userId, type, parameters)` takes no title and no body. The REST payload and the SignalR push carry the same two fields. **The client renders the text**, through the same `react-i18next` machinery it already uses for every other string on the page.

Emails are the opposite and stay that way: they are rendered server-side, localized with `IStringLocalizer` from `User.Language` (ADR-BACK-EMAIL-002), because an email leaves the platform and there is no client on the other end to render anything.

**What it replaced.** `Notification.Title` and `Notification.Body` — English sentences composed inside outbox handlers (`"Achievement Unlocked"`, `$"You've earned a certificate for \"{CourseTitle}\"."`) and stored, already rendered, in the database.

**Why:**
- **The server has no business choosing the language of the UI.** It does not know which language the tab is in — only which one the user last saved. The client does know, and it re-renders the moment the user switches. A stored English sentence never can.
- **Stored prose is frozen prose.** Rows written before a wording change keep the old wording forever; rows written before a *language* change keep the old language forever. With type + params, yesterday's notification re-renders in today's language, in today's phrasing.
- **The notification table stops being a translation table.** `Title`/`Body` were `varchar(200)`/`varchar(500)` of duplicated text — 50 rows per user, each carrying a sentence the client could have produced for free.
- **The client already owns the vocabulary.** The achievement `FIRST_LESSON` has a name in `achievements.json` in both languages. The server sending "First Step" would be the server guessing at a translation the client had all along — so it sends the code, and the client looks it up.

**The contract:**

| Type | Parameters | Rendered by the client from |
|---|---|---|
| `AchievementEarned` | `{ code }` | `notifications:items.AchievementEarned.body` + `achievements:meta.{code}.name` |
| `CertificateReady` | `{ courseTitle }` | `notifications:items.CertificateReady.body` |
| `InstructorApproved` | — | the type alone |
| `InstructorRejected` | — | the type alone |
| `RoleAssigned` | `{ role }` | `notifications:items.RoleAssigned.body` + the role name |
| `RoleRemoved` | `{ role }` | `notifications:items.RoleRemoved.body` + the role name |

**Why the role travels as a parameter and not as a type.** `RoleAssigned` covers `Instructor` and `Admin`
with one type, the same way `AchievementEarned` covers every achievement with one. A type per role would
grow the enum every time a role is added and teach the server a distinction the client is better placed
to draw. `InstructorApproved` stays separate and is not a duplicate of `RoleAssigned{Instructor}`: one
says an application was reviewed, the other says an admin acted directly. They are different sentences to
the person reading them, and only the first has an application behind it.

**Rejected alternatives:**
- *Localizing the notification text server-side, like emails.* Renders into `User.Language` at write time and freezes it. A user who switches the interface to English still sees a Ukrainian bell.
- *Sending both — type/params **and** a pre-rendered fallback string.* Two sources of truth, and the fallback is the one that rots. If the client can render it, the string is redundant; if it cannot, the string is a bug to fix, not to paper over.
- *Storing the params as separate typed columns.* Every new notification type would need a migration. `jsonb` costs nothing and Postgres can still query into it.

**Consequences:**
- Adding a notification type = an enum value + an i18n entry on each side. No server-side copywriting.
- Notifications written before this ADR lost their text with the dropped columns; they render from their type, with any `{{param}}` placeholder empty. Acceptable: the bell keeps at most 50 rows per user and they age out fast.
- A notification whose `Parameters` will not parse renders from the type alone rather than failing the query — a corrupt row must not take the bell down with it.

---

## ADR-BACK-NOTIF-002: A role change is announced twice — by email and by the bell — and the bell is not the backup

**Decision:** Every change to a user's roles raises `UserRoleChangedDomainEvent`, and its handler enqueues
**both** an email and an in-app notification (`RoleAssigned` / `RoleRemoved`, carrying `{ role }`). This
applies to `Instructor` and `Admin` alike — the handler never looks at which role it is.

Approving or rejecting an instructor application already did both (`InstructorApproved` /
`InstructorRejected`); this extends the same treatment to the direct grant/revoke an admin performs from
the users table, which previously sent only an email.

**Why both, when the email already went out:**
- **They arrive at different moments, to different people.** The email reaches someone who is away from
  the platform. The bell reaches someone who is *in* it — and who can act on the news immediately.
  Neither is a fallback for the other; the same event simply has two audiences.
- **Only the bell can open a door.** A notification is a click away from the instructor dashboard the
  role just unlocked. An email can carry a link, but it cannot put it in front of someone who is already
  looking at the app.
- **The email is not reliable enough to be the only channel.** Transactional mail lands in spam, is
  filtered by corporate gateways, or is simply never opened. This is not a hypothetical: the platform
  keeps `Notifications` for exactly the events where "we sent an email" is not the same as "the user
  knows".
- **Losing a role is the half that matters.** An instructor whose dashboard disappears with no
  explanation reads it as a bug. `RoleRemoved` is the only place the platform tells them what happened.

**Why `Admin` is announced too, rather than assumed obvious:** the email already went out for every role,
because the handler was always written generically. Making the bell selective would mean writing a rule
to *suppress* a notification — more code to say less. And "your admin role was removed" is worth saying
to whoever it happens to.

**The gap this does not close, and which the client must:** roles live in the JWT
(ADR-BACK-AUTH-008), and nothing revokes a token when they change. The notification arrives over SignalR
in real time, but the recipient's access token still carries the old roles until it is refreshed — up to
15 minutes, or the next 401, whichever comes first. `RefreshTokenCommandHandler` re-reads the roles from
the database, so a refresh is all it takes. **A client that deep-links `RoleAssigned` straight to the
instructor dashboard will bounce off its own route guard** unless it refreshes the token when the
notification arrives. That is the client's move to make; the server cannot make it from here.

**Rejected alternatives:**
- *Bell only, drop the role-change email.* An email is the only channel that reaches a user who is not
  coming back on their own — which is exactly the person a new instructor role is meant to bring back.
- *A notification type per role* (`InstructorRoleAssigned`, `AdminRoleAssigned`). Grows the enum per role
  and pushes a naming decision to the server, against ADR-BACK-NOTIF-001, which keeps the varying part in
  the parameters.
- *Reusing `InstructorApproved` for a direct grant.* It would be false: nothing was applied for and
  nothing was approved. The client would have no way to tell the two stories apart, and one of them
  would be a lie.
- *Revoking the user's tokens on role change so the new role takes effect at once.* It logs the user out
  mid-session to deliver good news, and the same refresh that fixes it happens within 15 minutes anyway.
  Revisit only if a role change ever needs to take effect immediately for security reasons — a revoked
  `Admin` keeping the claim for up to 15 minutes is the case to watch.
