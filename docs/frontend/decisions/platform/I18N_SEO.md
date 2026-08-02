# Learnix — Frontend Architecture Decision Records (I18n & SEO)

---

## ADR-FRONT-INTL-001: Localization with react-i18next

**Context:** The app ships in English and Ukrainian from day one, so string handling has to support
runtime language switching, not just static text.

**Decision:**
- All UI text is stored in JSON files, one per namespace (page/domain), separately for each language.
- Components and hooks use the `useTranslation(namespace)` hook from `react-i18next`.
- Supported languages: `en` (English) and `uk` (Ukrainian). Fallback is `en`.
- Current language choice is persisted to `localStorage` via a Zustand `locale.store`.

**Why:**
- `react-i18next` is the industry standard for React localization.
- Using namespaces maps cleanly to our page/domain structure (1:1 with the old `const/localization/` structure), keeping JSON files small and organized.
- It correctly supports pluralization rules for complex languages like Ukrainian out of the box via CLDR (`Intl.PluralRules`).
- `LanguageDetector` automatically detects the language from `localStorage` or the browser.
- `interpolation.escapeValue: false` — React escapes by default, avoiding double escaping.
- Static JSON imports (not lazy-loading) remain acceptable at the app's current namespace count, with zero added latency.

**Alternatives:**
- **Static TS const-dictionaries:** Convenient for one language, but doesn't support runtime switching.
- **react-intl (FormatJS):** Uses a more formal ICU format, which is overkill for our simple needs.
- **Lingui:** Has compile-time extraction and better DX, but the setup is more complex for a solo project.
- **JSON without a library:** Requires building a custom context and provider.

**Consequences:**
- New pages require a new JSON file in both `en/` and `uk/` directories, registered in `i18n/config.ts` under `resources`.
- New strings with parameters must use i18next interpolation `{{variable}}`, not JS string templates or functions.
- Developers must not use `i18n.t()` directly in components — always use the `useTranslation` hook.

---

## ADR-FRONT-INTL-002: Centralized Page Metadata via a Single `<Seo />` Component

**Context:** Each public page used to render its own `react-helmet-async` `<Helmet>` block for `<title>`,
description and Open Graph/Twitter tags, private layouts added a `noindex` robots meta inline, and
`robots.txt` / `sitemap.xml` were static files listing only the top-level routes.

**Decision:**
- Every page goes through one component, `components/common/seo/Seo.tsx`, instead of a page-local
  `<Helmet>` block. It centralizes `<title>`, description, canonical URL, the Open Graph and Twitter
  Card sets, an optional `noIndex`, and any JSON-LD structured data.
- Canonical URLs drop the query string, so a filtered or paginated view (e.g. the catalog with
  `?page=2&isFree=true`) consolidates onto its base route instead of splitting rank across
  near-duplicates.
- `robots.txt` and `sitemap.xml` are generated at build time from the live public catalog (published
  courses and instructors) instead of being hand-maintained static files, and degrade to a static
  route list if the API is unreachable during the build.
- The static fallback tags in `index.html` are stripped once React mounts, since React does not
  deduplicate the tags it renders against tags already present in the document head.

**Why:**
- Every page hand-rolling its own `<Helmet>` block let the tag set drift — some pages missed
  canonical or `og:url` entirely, and there was no single place to fix a systemic gap once found.
- A static sitemap listing only the top-level routes told search engines nothing about the actual
  catalog content behind them.
- Leaving the `index.html` fallback tags in place alongside the React-rendered ones produces two
  conflicting `og:title` / `og:url` tags per page, and a scraper reads whichever comes first.

**Alternatives:**
- Keep per-page `<Helmet>` blocks and patch each gap as it surfaces — rejected, since that is the
  pattern that caused the drift in the first place.
- Serve `sitemap.xml` directly from the backend API — rejected, since a cross-origin sitemap needs
  both hosts verified in Search Console and would live on a different domain than the pages it lists.

**Consequences:**
- A new public page renders `<Seo title={...} description={...} />`; it must not use `<Helmet>`
  directly.
- A new private page or layout uses `<Seo noIndex />` (or the layout's own `robots` meta), and is
  added to the sitemap generator's disallow list.
- A new public route that should be indexed must be added to the generator's static route list.
- The production build needs the site's own base URL configured, or the generated sitemap and Open
  Graph tags fall back to a local development URL.
- Non-JS scrapers (social-media link previews) still only ever see the fallback tags baked into
  `index.html` at build time, since the app has no server-side rendering — a known limitation, not
  solved by this decision, tracked separately in `docs/TECH_DEBT.md`.

---

## ADR-FRONT-INTL-003: Zod Validation Localization

**Context:** Zod's default error messages are English-only and hardcoded into the schema call site,
which doesn't fit an app that switches language at runtime.

**Decision:**
Validation error messages are localized using `zod-i18n-map` integrated with `i18next`. The global error map is configured once during app initialization, allowing Zod schemas to be completely free of translation logic.

**Why:**
- Separation of concerns: Schemas describe the shape of the data, not the UI presentation or languages.
- Prevents schema files from becoming bloated with boilerplate `t()` calls.
- Easy to manage standard validation messages (e.g., "Required field", "Invalid email") across the entire app.

**Alternatives:**
- *Inline translations*: `z.string({ message: t('validation.required') })`. Discarded because it tightly couples validation logic to the React lifecycle (requiring hooks) and heavily clutters the schema definitions.
- *React Hook Form Resolver mapping*: Catching errors at the `hookform/resolvers/zod` layer and translating them there. Discarded because `zod-i18n-map` solves this natively at the Zod core level.

---

## ADR-FRONT-INTL-004: Consolidating Generic Translations into a Common Namespace

**Context:** Terms like "Cancel" or "Save" show up on nearly every page; letting each page-level
namespace define its own copy duplicates the string (and its Ukrainian translation) dozens of times.

**Decision:**
- Highly generic and frequently used terms (e.g., "Cancel", "Save", "Delete", "Status", "Courses", "Email", etc.) are extracted into a shared `common.json` namespace.
- `common.json` is organized into logical groups such as `actions`, `status`, `navigation`, `roles`, `footer`, `upload`, and a handful of loose top-level keys for one-off shared strings.
- Components use `t('common:actions.cancel')` instead of defining duplicated keys in their respective page-level JSON files.

**Why:**
- Prevents massive duplication across the rest of the namespace JSON files.
- Ensures consistency in terminology across the entire application for both English and Ukrainian (e.g., standardizing "Cancel" as "Скасувати").
- Reduces the effort required for translators and developers when adding generic UI components like tables and modal dialogs.
- Allows page-specific namespaces to remain focused strictly on domain-specific terminology (e.g., `Course Management`, `Instructor Motivation`).

**Alternatives:**
- **Strict isolation (No shared namespace):** Every page has its own "Cancel" key (e.g., `admin:btnCancel`, `instructor:btnCancel`). This was the original approach but led to unnecessary bloat and maintenance overhead.
- **Global flat file:** Storing all translations in one massive file. This would negatively impact performance (loading all translations at once) and cause naming collisions.

**Consequences:**
- Before adding a new string to a specific namespace, developers should check `common.json` to see if a generic term already exists.
- The `common` namespace is small enough to be loaded globally if needed, or included on almost every page.
