# Backend Security & Code Audit

Perform a thorough security and correctness audit of the Learnix .NET 8 backend.

**Scope:** $ARGUMENTS (if empty — audit the entire `Learnix.Backend/` tree)

---

## Step 1 — Gather context

Read the following files before starting:
- `ARCHITECTURE.md`
- `DECISIONS.md`
- `DATA_MODEL.md`
- `.claude/skills/backend-standards/SKILL.md`

Then scan the target scope with Glob/Grep to collect all `.cs` files relevant to the audit.

---

## Step 2 — Run the audit checklist

Work through every category below. For each finding write:

```
[SEVERITY] CATEGORY › File:Line — description + recommendation
```

Severities: 🔴 CRITICAL · 🟠 HIGH · 🟡 MEDIUM · 🔵 LOW · ℹ️ INFO

---

### A. Authentication & Authorization

1. **Missing `[Authorize]`** — Every controller action that touches user data must have `[Authorize]` or inherit it from the controller. Check for unprotected endpoints that should be protected.
2. **Role-based gaps** — Actions restricted to `Admin` or `Instructor` must use `[Authorize(Roles = "...")]` or a policy. Check that no Student can reach Instructor/Admin endpoints.
3. **Ownership / IDOR** — In every handler that fetches or mutates a resource owned by a user (course, enrollment, review, message, certificate…), verify the handler reads `ICurrentUserService` (or equivalent) and compares the resource owner's ID to the caller's ID. Missing check = IDOR vulnerability.
4. **JWT configuration** — In `JwtSettings` / `Program.cs`: verify `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` are all `true`. Check token expiry is ≤ 15 min for access tokens.
5. **Refresh token security** — Confirm tokens are hashed before storage (never stored plain), rotated on every use, and invalidated on logout. Check for token-family / reuse-detection logic.
6. **HttpOnly cookie** — Refresh token cookie must have `HttpOnly = true`, `Secure = true`, `SameSite = Strict/Lax`. Check `CookieOptions` in the auth infrastructure.
7. **Email confirmation bypass** — Confirm that login is rejected (or features are restricted) until email is confirmed.

---

### B. Input Validation & Injection

8. **Missing FluentValidation** — Every Command and Query must have a corresponding `*Validator` registered via assembly scan. List any Command/Query without a validator.
9. **Raw SQL / LINQ injection** — Search for `FromSqlRaw`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated` calls. Parameterised interpolation (`$""`) is safe; string concatenation is not.
10. **Mass assignment** — Check that controllers only accept typed DTOs / commands, never `dynamic` or `Dictionary<string, object>`. Confirm no entity property is bound directly from request without validation.
11. **File upload validation** — In `UploadsController` and blob/SAS logic: verify allowed MIME types, max size limits, and that the file extension is validated server-side (not just client-side).
12. **Open redirect** — Check any redirect-after-login or `returnUrl` handling for open redirect (must be validated as a local URL).

---

### C. Sensitive Data Exposure

13. **Secrets in source** — Grep for connection strings, API keys, secret values hardcoded in `.cs`, `appsettings.json` (non-example), or `launchSettings.json`. Flag anything that should be in `.env` / secrets vault.
14. **Logging sensitive data** — In `LoggingBehavior` and anywhere `ILogger` is used: check that passwords, tokens, PII (email, full name) are not logged at Debug/Info level.
15. **Response over-sharing** — Check response DTOs for fields that should not be returned to the caller (e.g., `PasswordHash`, `RefreshTokenHash`, internal IDs not needed by the client).
16. **Exception details** — Confirm `ExceptionHandlingMiddleware` never returns stack traces or internal messages in production. Check `ProblemDetails` payloads.

---

### D. Architecture & Pattern Correctness

17. **Result<T> not checked** — Every call to a method returning `Result` / `Result<T>` must check `.IsFailed` or use `.Bind`/`.Match`. Flag places where the result is ignored.
18. **Throws instead of Result.Fail** — Business-rule errors must return `Result.Fail(...)`, not throw exceptions (except truly exceptional conditions). Flag handlers that `throw` for expected errors.
19. **Repository bypassed** — Handlers must query via Specification + repository, not inject `DbContext` directly (unless in Infrastructure layer, which is allowed).
20. **Soft delete filter bypassed incorrectly** — `.IgnoreQueryFilters()` must only be used deliberately (admin views, seeder). Flag any use in regular user-facing handlers.
21. **N+1 queries** — Look for loops that call the repository / await inside `foreach`. Flag any obvious N+1 patterns without `.Include()` or batch loading.
22. **Async correctness** — Check for `.Result`, `.Wait()`, or `async void` (except event handlers). Flag blocking calls on async methods.
23. **Layer violations** — Application layer must not reference `Microsoft.EntityFrameworkCore`, `Npgsql`, or any infrastructure namespace. Infrastructure layer must not reference API layer.

---

### E. API Surface & HTTP Security

24. **CORS policy** — In `Program.cs` / extension methods: verify CORS does not allow `AllowAnyOrigin` + `AllowCredentials` together (browser blocks this and it is insecure). Origins should be explicitly listed in production config.
25. **Rate limiting** — Check that auth endpoints (login, register, forgot-password, resend-email) are rate-limited to prevent brute force.
26. **Security headers** — Verify `SecurityHeadersMiddleware` sets at minimum: `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`.
27. **HTTP method correctness** — Mutations (create/update/delete) must use POST/PUT/PATCH/DELETE — never GET. Verify no state-changing logic behind a GET endpoint.
28. **Pagination without limits** — Any endpoint returning a list must enforce a max `PageSize` to prevent DoS via huge page requests.

---

### F. Infrastructure & Configuration

29. **MongoDB input** — If user-supplied strings are used in MongoDB filter expressions, check for NoSQL injection (avoid string interpolation in filter builders — use typed builders).
30. **Redis cache poisoning** — Check that cache keys include the user's ID where data is user-specific; public caches must never contain private data.
31. **Background jobs / hosted services** — Check `IHostedService` implementations for unhandled exceptions that silently kill the service loop. Verify cancellation tokens are respected.

---

## Step 3 — Summary table

After the per-finding list, output a markdown table:

| # | Severity | Category | File | Issue (short) |
|---|----------|----------|------|---------------|

Then provide a **Priority action list** — the top 5 issues to fix first, with one-line fix instructions each.

---

## Output format rules

- Group findings by category (A–F).
- If a category has **no issues**, write `✅ No issues found in this category.`
- Be specific: always include the file path and line number (or method name) for each finding.
- Do not repeat the same finding more than once.
- Do not invent issues — only report what you can verify in the actual code.
