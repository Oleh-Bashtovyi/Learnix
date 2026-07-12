# Architectural Decision Records (ADR)

This directory records *why* the backend is built the way it is — the context behind each significant
decision and the alternatives that were rejected.

Files are grouped from the most general to the most specific: `platform/` holds the decisions
everything else rests on, `features/` the user-facing domains, `operations/` how the system is
built, shipped and observed. Start at the top of `platform/` and go down only as far as you need.

## Decisions Index

### `platform/` — the foundation every feature depends on

| Document | What it decides |
|---|---|
| [Architecture & CQRS](platform/ARCHITECTURE.md) | Layering, MediatR pipeline, `Result<T>`, error → HTTP mapping |
| [Domain Model](platform/DOMAIN.md) | Entities, aggregate, invariants, lifecycle — read top-down |
| [Infrastructure & Data Access](platform/INFRA.md) | PostgreSQL + MongoDB, Redis cache, EF interceptors, Outbox, repositories, PII masking |
| [Migrations & Seeding](platform/MIGRATIONS.md) | `Learnix.DbMigrator`, seed data, seed assets |
| [Authentication & Security](platform/AUTH.md) | JWT, refresh rotation, roles, Google OAuth, email confirmation |
| [Blob Storage & Uploads](platform/BLOB.md) | SAS upload flow, blob path format, deletion via Outbox |

### `features/` — the user-facing domains

| Document | What it decides |
|---|---|
| [LMS Core](features/LMS.md) | TPH lesson types, progress tracking and course completion |
| [AI Chat](features/CHAT.md) | `IAiChatProvider` abstraction, MongoDB sessions, scoped sessions, rolling context windows |
| [Payments](features/PAYMENT.md) | `Payment` as its own entity, atomic payment + enrollment |
| [Achievements](features/ACHIEVEMENTS.md) | Outbox-driven evaluation, idempotent counters, unlock deduplication |
| [Certificates](features/CERTIFICATES.md) | QuestPDF, QR codes via QRCoder, synchronous generation |
| [Direct Messaging](features/MESSAGING.md) | PostgreSQL over MongoDB, REST history + SignalR delivery, unread counts |
| [In-App Notifications](features/NOTIFICATIONS.md) | A notification is data, not a sentence |
| [Course Reviews](features/REVIEWS.md) | Denormalized rating arithmetic, visibility rules |
| [Users, Deletion & Retention](features/USERS.md) | Soft delete, a promised date, anonymization at the end |
| [Email](features/EMAILS.md) | MailKit + RazorLight + PreMailer, localization via `.resx` |

### `operations/` — how it runs

| Document | What it decides |
|---|---|
| [CI/CD Pipelines](operations/CICD.md) | Separate backend/frontend workflows, deploy order, SonarCloud |
| [Logging](operations/LOGGING.md) | Serilog, request traceability via `LogEnrichmentMiddleware`, Seq |
| [Forwarded Headers (Proxies)](operations/FORWARDED_HEADERS.md) | Which `X-Forwarded-*` headers to trust behind a proxy — *not yet written as ADRs* |

> Decisions about the **repository as a whole** — the monorepo layout, the commit convention — are
> not backend decisions and live in [`docs/decisions/`](../../decisions/README.md).

> **Looking for the endpoints?** They are not here. The API surface lives in
> [`docs/backend/ENDPOINTS.md`](../ENDPOINTS.md), generated from the controllers and verified
> against them in CI. An ADR records a decision; it does not keep a copy of the routing table.

## Conventions

- **Every backend ADR is `ADR-BACK-<SCOPE>-NNN`**, where `SCOPE` is the file's topic (`INFRA`, `CHAT`,
  `LMS`, …). No exceptions — the register used to mix that spelling with a shorter `ADR-<SCOPE>-NNN`
  form, and nothing noticed. Now `npm run check:adr` fails if an id is cited — in prose or in a C#
  comment — with no ADR heading behind it.
- **Numbering is scoped per file**, restarting at `001` in each document, so moving a file between
  folders never renumbers anything — read the file, take the next free number.
- **Numbers are never reused.** Gaps mean an ADR was removed, not that one is missing.
- **A decision that no longer describes reality is removed, not left as a tombstone** — the rejected
  approach lives on in the *Alternatives* section of whatever replaced it, and the full text stays in
  git history.

## Proposing a New Decision

1. Add the ADR to the existing file for its scope. Only start a new file for a genuinely new topic.
2. For a new file, copy [`TEMPLATE.md`](TEMPLATE.md), place it in the right folder, and register it
   in the index above.
3. Open a PR — the decision is what gets reviewed, not just the code that follows from it.
