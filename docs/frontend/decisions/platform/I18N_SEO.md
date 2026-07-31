# Learnix — Frontend Architecture Decision Records (I18n & SEO)

> Format: Decision → Why → Alternatives.

---

## ADR-FRONT-INTL-001: Localization with react-i18next

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
- Static JSON imports (not lazy-loading) are acceptable for ~20 namespaces with zero latency.

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

## ADR-FRONT-INTL-002: Client-Side SEO Strategy

**Decision:**
Basic SEO optimization is implemented via `react-helmet-async` in a client-side SPA without Server-Side Rendering (SSR). This approach covers 6 levels:
1. **`index.html`:** Contains static fallback tags (`<title>`, `<meta name="description">`, OG tags, Twitter Card) that crawlers see before JS executes.
2. **`react-helmet-async`:** Overwrites tags at the page level during React render. The `<HelmetProvider>` wraps the app in `main.tsx`.
3. **Public Pages:** `LandingPage`, `CourseCatalogPage`, `FaqPage` use `t('seo.title')` / `t('seo.description')` from their respective i18n namespaces. `CourseDetailPage` builds tags dynamically from `CourseDetailDto` (title, description[:160], coverImageUrl).
4. **Private Layouts:** `AdminLayout` and `InstructorLayout` render `<meta name="robots" content="noindex,nofollow">` so crawlers won't index these pages even if they reach them.
5. **`public/robots.txt`:** Blocks crawlers from `/admin/`, `/instructor/`, and all private student routes.
6. **`public/sitemap.xml`:** Contains static public URLs (`/`, `/courses`, `/faq`). Dynamic course URLs would require a build-time generator or server endpoint (not implemented in v1).

**Why:**
- Learnix is a Vite SPA. Migrating to Vite SSR or Remix would take a week of work and significantly complicate deployment.
- Googlebot executes JavaScript, making `react-helmet-async` effective for Google.
- For social networks (e.g., `og:image` in Twitter, Facebook), the fallback tags in `index.html` are sufficient for the landing page. For course links, Googlebot will use JS anyway.
- For an MVP portfolio project, this client-side approach delivers 80% of the SEO value for 5% of the effort compared to setting up Node.js SSR hosting.

**Alternatives:**
- **Vite SSR / Vike:** Full control, but complex deployment (requires a Node.js server, not static hosting).
- **Remix or Next.js:** Best for SEO, but requires a complete refactor of routing and state management.
- **`vite-plugin-prerender`:** Statically prerenders known routes at build time. Makes sense for static public pages (`/`, `/faq`), but not for dynamic courses.

**Consequences:**
- When adding a new public page, add `<Helmet>` with `<title>` and `<meta name="description">` to the component; add `seo.title` / `seo.description` keys to the respective i18n namespace.
- When adding a new private page or layout, add `<meta name="robots" content="noindex,nofollow">` to the layout component.
- `robots.txt` must be updated manually when introducing new private route paths.

> **Superseded in part by ADR-FRONT-INTL-005.** Pages no longer use `<Helmet>` directly (they use
> `<Seo />`), `robots.txt` and `sitemap.xml` are generated at build time, and the claim that the
> `index.html` fallback tags are "sufficient" for social networks was wrong: non-JS scrapers only
> ever see the landing page's tags, so shared course links never carried the course's own preview.

---

## ADR-FRONT-INTL-005: Canonical URLs, Structured Data and a Single `<Seo />` Component

**Decision:**
All per-page metadata goes through one component — `components/common/seo/Seo.tsx` — instead of
hand-rolled `<Helmet>` blocks. It renders `<title>`, `description`, `<link rel="canonical">`, the
Open Graph and Twitter Card sets, an optional `robots: noindex,nofollow`, and any JSON-LD blocks.

1. **Canonical URLs:** every public page emits one. The default is the current pathname *without*
   the query string, so the filtered/paginated catalog (`/courses?page=2&isFree=true`) consolidates
   onto `/courses` instead of splitting rank across near-duplicates.
2. **Structured data** (`utils/seo.ts` builders): `Course` + `BreadcrumbList` on the course page,
   `FAQPage` on `/faq`, `Organization` + `WebSite` (with a `SearchAction`) on the landing page.
   `aggregateRating` is only emitted for a course with at least one review — a zero-review rating is
   a structured-data error. `courseWorkload` is omitted because lessons carry no duration.
3. **`noindex` on non-pages:** the 404 page and the not-found / failed-to-load states of the course
   and instructor pages. Static hosting can only answer `200` for unknown SPA routes, so `noindex`
   is the only thing that keeps them out of the index.
4. **`<html lang>`** is synced to the active language by an `i18n.on('languageChanged')` listener in
   `i18n/config.ts`.
5. **Absolute URLs** come from `env.SITE_URL` (`VITE_SITE_URL`, falling back to the runtime origin).
   The same variable feeds `%VITE_SITE_URL%` in `index.html` via the `learnix:inject-site-url` Vite
   plugin — Vite's own `%VAR%` substitution silently leaves the placeholder when the var is unset,
   which would ship a broken `og:image`.
6. **`robots.txt` + `sitemap.xml` are generated** by `scripts/generate-seo-files.mjs` (`prebuild`).
   The sitemap includes every published course and instructor, pulled from the public catalog API;
   if the API is unreachable it degrades to the static routes rather than failing the build. Both
   files are gitignored — they are build output, not source.
7. **The `index.html` fallback tags are stripped on boot.** They are marked `data-seo-fallback` and
   removed in `main.tsx`. React 19 hoists the tags `<Seo />` renders *without* deduplicating them
   against `index.html`, so leaving them in place gives every page two conflicting `og:title` /
   `og:url` tags — and a scraper reads the first one, i.e. always the landing page's.

**Why:**
- Duplicate/near-duplicate URLs and missing canonicals are the highest-leverage SEO defect on a
  catalog-shaped site; a canonical tag is ~10 lines and fixes it.
- `Course` and `FAQPage` rich results are the whole point of structured data for an LMS — ratings,
  price and instructor rendered directly in the SERP.
- A static three-URL sitemap advertised none of the actual content. Generating it at build time is
  the only option that keeps the frontend on static hosting.

**Alternatives:**
- **Per-page `<Helmet>` blocks (the status quo):** every page reinvents the tag set and forgets
  canonical/`og:url`; there was no single place to fix a systemic bug.
- **Serving `sitemap.xml` from the API:** a cross-origin sitemap needs both hosts verified in Search
  Console, and the sitemap would live on a different domain than the pages it lists.
- **Hardcoding the production domain:** breaks preview environments and local builds.

**Consequences:**
- New public page → render `<Seo title={...} description={...} />`; do not use `<Helmet>` directly.
- New private page/layout → `<Seo noIndex />` (or the layout-level `robots` meta), *and* add the
  route to `DISALLOWED` in `scripts/generate-seo-files.mjs`.
- A new public route that should be indexed must be added to `STATIC_ROUTES` in the same script.
- `VITE_SITE_URL` must be set in the production build environment (GitHub Actions repo variable).
  Without it the build falls back to `http://localhost:5173` and the sitemap/OG tags are useless.
- **Still unsolved:** non-JS scrapers (Facebook, LinkedIn, Slack) see only the landing page's
  fallback tags, so a shared course link shows a generic preview. Fixing that needs prerendering or
  an edge function — tracked in `docs/TECH_DEBT.md`.

---

## ADR-FRONT-INTL-003: Zod Validation Localization

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

**Decision:**
- Highly generic and frequently used terms (e.g., "Cancel", "Save", "Delete", "Status", "Courses", "Email", etc.) are extracted into a shared `common.json` namespace.
- The `common.json` file is organized into logical groups such as `actions`, `status`, `navigation`, and `general`.
- Components use `t('common:actions.cancel')` instead of defining duplicated keys in their respective page-level JSON files.

**Why:**
- Prevents massive duplication across 20+ namespace JSON files.
- Ensures consistency in terminology across the entire application for both English and Ukrainian (e.g., standardizing "Cancel" as "Скасувати").
- Reduces the effort required for translators and developers when adding generic UI components like tables and modal dialogs.
- Allows page-specific namespaces to remain focused strictly on domain-specific terminology (e.g., `Course Management`, `Instructor Motivation`).

**Alternatives:**
- **Strict isolation (No shared namespace):** Every page has its own "Cancel" key (e.g., `admin:btnCancel`, `instructor:btnCancel`). This was the original approach but led to unnecessary bloat and maintenance overhead.
- **Global flat file:** Storing all translations in one massive file. This would negatively impact performance (loading all translations at once) and cause naming collisions.

**Consequences:**
- Before adding a new string to a specific namespace, developers should check `common.json` to see if a generic term already exists.
- The `common` namespace is small enough to be loaded globally if needed, or included on almost every page.
