# Architectural Decision Records (ADR)

This directory records *why* the frontend is built the way it is — the context behind each
significant decision and the alternatives that were rejected.

Files are grouped `platform/` (the tech choices every feature builds on) and `operations/` (how the
app is built and shipped). Backend ADRs use a third `features/` group for user-facing domains;
frontend decisions are almost entirely cross-cutting, so there isn't an equivalent group here.

## Decisions Index

### `platform/` — the foundation every feature depends on

| Document | What it decides |
|---|---|
| [Architecture](platform/ARCHITECTURE.md) | Layer-based structure, page co-location, routing & guards, tooling stack, the `APP_ROUTES` dictionary |
| [API & State Management](platform/API.md) | Axios + queued token refresh, Zustand/TanStack Query boundary, React Query defaults, the realtime hub, env vars, DTO strategy, pagination |
| [Authentication](platform/AUTH.md) | Token storage & silent refresh, OTP email verification, password reset, logout, role-based routing, mid-session role-change refresh, email-confirmation gating |
| [UI & Styling](platform/UI.md) | Tailwind + shadcn/ui, safe markdown rendering, the shadcn primitives catalog, surface tokens, shared state panels, form-field tokens |
| [Forms](platform/FORMS.md) | React Hook Form wrappers, Zod as the source of truth, server-to-client validation mapping, form vs. global errors |
| [Internationalization & SEO](platform/I18N_SEO.md) | react-i18next namespaces, the `<Seo />` component, structured data, generated `robots.txt`/`sitemap.xml` |

### `operations/` — how it's built and shipped

| Document | What it decides |
|---|---|
| [Linting & Formatting](operations/LINTING_FORMATTING.md) | ESLint flat config, Prettier as the sole formatter, import sorting, Tailwind/React strictness plugins |
| [Deployment](operations/DEPLOYMENT.md) | Static hosting (no Node server), SPA client-side routing fallback |

## Conventions

- **Every frontend ADR is `ADR-FRONT-<SCOPE>-NNN`**, where `SCOPE` is the file's topic (`AUTH`, `UI`,
  `API`, …). No exceptions — `npm run check:adr` fails the build if an id is cited anywhere (prose or
  a code comment) with no ADR heading behind it.
- **Numbering is scoped per file**, restarting at `001` in each document, so moving a file between
  `platform/` and `operations/` never renumbers anything — read the file, take the next free number.
- **Numbers are never reused.** Gaps mean an ADR was removed, not that one is missing.
- **A decision that no longer describes reality is removed, not left as a tombstone.** Don't mark an
  ADR "Superseded" or "Cancelled" and leave both the old and new text side by side — replace it with a
  single ADR describing what changed and why; the old text stays recoverable in git history, not in
  the document.

## Proposing a New Decision

1. Add the ADR to the existing file for its scope. Only start a new file for a genuinely new topic.
2. For a new file, copy [`TEMPLATE.md`](TEMPLATE.md), place it in `platform/` or `operations/`, and
   register it in the index above.
3. Open a PR — the decision is what gets reviewed, not just the code that follows from it.
