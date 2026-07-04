# Frontend Security & Code Audit

Perform a thorough security and correctness audit of the Learnix React 19 + TypeScript frontend.

**Scope:** $ARGUMENTS (if empty — audit the entire `learnix-client/src/` tree)

---

## Step 1 — Gather context

Read the following files before starting:
- `ARCHITECTURE_FRONTEND.md`
- `DECISIONS_FRONTEND.md`
- `.claude/skills/frontend-standards/SKILL.md`

Then scan the target scope with Glob/Grep to collect all `.ts` / `.tsx` files relevant to the audit.

---

## Step 2 — Run the audit checklist

Work through every category below. For each finding write:

```
[SEVERITY] CATEGORY › File:Line — description + recommendation
```

Severities: 🔴 CRITICAL · 🟠 HIGH · 🟡 MEDIUM · 🔵 LOW · ℹ️ INFO

---

### A. Cross-Site Scripting (XSS)

1. **`dangerouslySetInnerHTML`** — Search for every usage. Each one must be sanitized with DOMPurify or equivalent before rendering. Unsanitized user content rendered this way = stored/reflected XSS.
2. **`eval` / `new Function` / `setTimeout(string)`** — Flag any dynamic code execution with user-controlled input.
3. **Unescaped URL params in markup** — Check if `useSearchParams()` or `useParams()` values are rendered directly into the DOM without encoding.
4. **`href` / `src` injection** — Links built from user data must be validated (`href` must start with `http`/`https`/`/`, never `javascript:`).
5. **`innerHTML` in vanilla DOM calls** — Look for `element.innerHTML =` assignments outside of React's virtual DOM.

---

### B. Sensitive Data Storage & Exposure

6. **Tokens in `localStorage`** — Access tokens must **not** be stored in `localStorage` (XSS-readable). Verify `auth.store.ts`: the access token should only live in memory (Zustand state). If it is in `localStorage`, that is a 🔴 CRITICAL finding.
7. **Sensitive data in `sessionStorage`** — Check if passwords, full tokens, or PII are stored in `sessionStorage`.
8. **Tokens in URLs** — Confirm that access tokens are never appended as query params (`?token=...`). They must go in `Authorization` headers only.
9. **Tokens in console logs** — Search for `console.log` calls that might print auth state, tokens, or user credentials.
10. **`VITE_` env vars** — All variables prefixed `VITE_` are exposed in the browser bundle. Flag any `VITE_` variable that contains a secret (API key, private token, signing secret). Public base URLs are fine.

---

### C. Authentication & Route Guard

11. **Unguarded private routes** — Every route under `pages/student/`, `pages/instructor/`, `pages/admin/` must be wrapped in a role-aware route guard. Check `router` / route config files for missing guards.
12. **Role escalation** — A Student must not be able to navigate to Instructor or Admin pages by manually changing the URL. Verify guard logic checks the role, not just `isAuthenticated`.
13. **Stale auth state after logout** — On logout: access token in memory must be cleared, refresh token cookie invalidated (via API call), and TanStack Query cache must be reset (`queryClient.clear()`). Check `LogoutCommand` flow.
14. **Google OAuth state parameter** — If Google login uses a redirect flow, verify the `state` parameter is generated and validated to prevent CSRF on the OAuth callback.
15. **Token refresh race condition** — In `axios.instance.ts`, confirm that concurrent 401 responses are queued and resolved with a single refresh call (no refresh storms). Flag if multiple simultaneous requests each trigger their own refresh.

---

### D. API Layer & HTTP Security

16. **Hardcoded base URLs / secrets** — Search for hardcoded `http://` / `https://` strings inside API files. Base URL must come from `import.meta.env.VITE_API_URL` (or equivalent env var).
17. **Missing error handling** — Every `axios` call in `*.api.ts` files must handle errors and propagate them in a consistent shape. Flag silent `catch(() => {})` that swallow errors.
18. **Credentials in request body** — Passwords must never be logged or attached to error-reporting payloads. Check form submit handlers and error boundaries.
19. **CORS pre-flight** — If any API call sets custom headers beyond `Content-Type` / `Authorization`, ensure the backend CORS policy allows them. Flag mismatches.
20. **Unsafe `withCredentials`** — `axios` is configured with `withCredentials: true` for cookie-based refresh. Verify this flag is **not** set globally for third-party API calls (e.g., CDN, analytics), only for the backend base instance.

---

### E. Form & Input Security

21. **Missing Zod validation** — Every `react-hook-form` form must have a `zodResolver` schema. Flag forms that use `register()` without a schema, leaving inputs unvalidated client-side.
22. **Weak Zod schemas** — Check schemas for:
    - Passwords: minimum length, complexity rules consistent with backend `PasswordRules.cs`
    - Emails: `.email()` validator present
    - Text fields: `.max()` to prevent oversized payloads
    - URLs: `.url()` where a URL is expected
23. **File upload type check** — In any file upload UI, verify accepted MIME types / extensions are restricted client-side (as a UX hint — server-side is the real gate).
24. **Uncontrolled inputs** — Flag `<input>` elements without `value`/`onChange` (uncontrolled) in forms that submit security-sensitive data.

---

### F. State Management & Data Leakage

25. **PII in Zustand persisted store** — Check every Zustand store that uses `persist` middleware. Persisted slices must not contain sensitive user data beyond what is strictly necessary (e.g., role, display name are fine; full profile objects, tokens are not).
26. **TanStack Query cache with sensitive data** — Queries that return payment info, certificates, or private messages should have `staleTime` and `gcTime` set to reasonable limits — not `Infinity` — to avoid indefinitely caching private data in memory.
27. **Global error boundary leaks** — Error boundaries must not render raw `error.message` or `error.stack` to the user (may expose internal API details). Use a generic user-friendly message.

---

### G. Dependency & Build Security

28. **`npm audit`** — Run `npm audit` in `learnix-client/` and report any HIGH or CRITICAL vulnerabilities. Include the package name, severity, and fix command.
29. **`dangerouslySetInnerHTML` from third-party libs** — Check if any third-party UI library used in the project renders raw HTML internally (check the lib's own docs / issues).
30. **Exposed source maps** — Check `vite.config.ts`: in production builds, `sourcemap` should be `false` or `'hidden'` to avoid exposing source code.
31. **`console.*` in production** — Flag `console.log/warn/error` calls that are not guarded by `import.meta.env.DEV`. These leak internal info in the production bundle.

---

### H. Localization & Hardcoded Strings (Code Correctness)

32. **Hardcoded UI strings** — Per project standard (FADR-012): all visible text must come from `src/const/localization/*.ts`. Flag any JSX that contains hardcoded Ukrainian or English text outside localization files.
33. **Missing localization keys** — If a feature has a localization file, verify all visible labels, placeholders, error messages, and ARIA labels are in that file — not inline.

---

## Step 3 — Automated checks to run

After the static analysis above, run these commands and include their output:

```bash
cd learnix-client
npx tsc --noEmit 2>&1 | head -60
npm audit --audit-level=high 2>&1 | tail -30
```

Include TypeScript errors and npm audit findings in the report.

---

## Step 4 — Summary table

After the per-finding list, output a markdown table:

| # | Severity | Category | File | Issue (short) |
|---|----------|----------|------|---------------|

Then provide a **Priority action list** — the top 5 issues to fix first, with one-line fix instructions each.

---

## Output format rules

- Group findings by category (A–H).
- If a category has **no issues**, write `✅ No issues found in this category.`
- Be specific: always include the file path and line number (or component/hook name) for each finding.
- Do not repeat the same finding more than once.
- Do not invent issues — only report what you can verify in the actual code.
