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
