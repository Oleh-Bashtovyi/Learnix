# Learnix — ADR: In-App Notifications

> The bell: what the server stores, what it pushes, and who chooses the words.

---

## ADR-NOTIF-001: A Notification Is Data, Not a Sentence

**Decision:** A notification carries **what happened** and **what it happened to** — never prose. `Notification` stores `Type` (the enum) and `Parameters` (a `jsonb` map of strings: `{"courseTitle": "React"}`, `{"code": "FIRST_LESSON"}`), and nothing else. `INotificationSender.SendAsync(userId, type, parameters)` takes no title and no body. The REST payload and the SignalR push carry the same two fields. **The client renders the text**, through the same `react-i18next` machinery it already uses for every other string on the page.

Emails are the opposite and stay that way: they are rendered server-side, localized with `IStringLocalizer` from `User.Language` (ADR-EMAIL-002), because an email leaves the platform and there is no client on the other end to render anything.

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

**Rejected alternatives:**
- *Localizing the notification text server-side, like emails.* Renders into `User.Language` at write time and freezes it. A user who switches the interface to English still sees a Ukrainian bell.
- *Sending both — type/params **and** a pre-rendered fallback string.* Two sources of truth, and the fallback is the one that rots. If the client can render it, the string is redundant; if it cannot, the string is a bug to fix, not to paper over.
- *Storing the params as separate typed columns.* Every new notification type would need a migration. `jsonb` costs nothing and Postgres can still query into it.

**Consequences:**
- Adding a notification type = an enum value + an i18n entry on each side. No server-side copywriting.
- Notifications written before this ADR lost their text with the dropped columns; they render from their type, with any `{{param}}` placeholder empty. Acceptable: the bell keeps at most 50 rows per user and they age out fast.
- A notification whose `Parameters` will not parse renders from the type alone rather than failing the query — a corrupt row must not take the bell down with it.
