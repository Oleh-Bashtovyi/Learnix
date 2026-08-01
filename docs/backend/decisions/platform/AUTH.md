# Learnix — ADR: Authentication & Authorization

> **Endpoints:** see [`docs/backend/ENDPOINTS.md`](../../ENDPOINTS.md) — one generated table for
> the whole API, verified against the controllers in CI. An ADR records a decision; it is not the
> place to keep a copy of the API surface.

---
## ADR-BACK-AUTH-001: JWT (short-lived) + Refresh Token (long-lived, HttpOnly cookie)

**Context:** the API needs to keep a user signed in for days without either asking for credentials
constantly or handing out one long-lived token that cannot be revoked if it leaks.

**Decision:** Authentication via token pair:
- **Access token (JWT):** 15 minutes, passed in `Authorization: Bearer` header
- **Refresh token:** 7 days, stored in an HttpOnly + Secure cookie (`learnix_refresh`, `Path=/api/auth`).
  `SameSite` is **not** `Strict` in production — see ADR-BACK-AUTH-007.

**Why:**
- A short-lived JWT minimizes the compromise window — even if stolen, it only lives for 15 minutes.
- HttpOnly cookie protects the refresh token from XSS (JavaScript has no access).
- Rotation: every refresh issues a new pair (access + refresh), the old refresh token is invalidated.
- The refresh token is stored hashed with a keyed HMAC (ADR-BACK-AUTH-017), so a database dump alone
  does not let an attacker verify a stolen token against it.

**Alternatives:**
- Session-based auth — simpler, but harder to scale horizontally without sticky sessions.
- JWT only (long-lived, no refresh) — dangerous, no revocation mechanism.
- OAuth2 + OpenID Connect (IdentityServer) — overkill for a monolithic LMS.

**Consequences:**
- Refresh token is stored in the `RefreshToken` table (PostgreSQL), hashed.
- On every refresh: old token is revoked, a new one is created.
- If a revoked token is used → ALL user's tokens are revoked (replay attack protection).
- Google OAuth: after successful OAuth flow, the server generates the JWT + refresh token pair.

---

## ADR-BACK-AUTH-002: ASP.NET Identity — inherit from IdentityUser, custom DbContext

**Context:** the platform needs password hashing, lockout, email confirmation and external logins, and
none of that is where building it from scratch would add value.

**Decision:** The User entity inherits from `IdentityUser<Guid>`.
We use our own `ApplicationDbContext`, not `IdentityDbContext`.
Instructor-specific data is NOT stored in claims.

**Instructor-specific data:** If needed — nullable fields on the User entity.
A separate `InstructorProfile` table is out of scope for v1.

**Why:**
- Identity provides out of the box: password hashing, token generation, lockout, email confirmation, external logins.
- Custom DbContext = full control over the schema, without redundant tables.
- Claims — for auth metadata (role, permissions), not for business data.

**Alternatives:**
- Fully out-of-the-box Identity (`IdentityDbContext`) — pulls in redundant tables, less control.
- No Identity at all — months spent writing auth manually with no real benefit.

---

## ADR-BACK-AUTH-003: Pure Identity roles instead of UserRole enum

**Context:** a user's role needs to be readable both from domain code and from
`[Authorize(Roles = ...)]`, and it started out as two separate representations of the same fact.

**Decision:** The `UserRole` enum was removed from Domain. Roles (Student / Instructor / Admin) live only in Identity (`AspNetRoles` + `AspNetUserRoles`). `Domain.Constants.Roles` is a static class with string constants for type-safe referencing.

**Why:**
- Duplicating an enum field on User and Identity roles — two sources of truth, inevitable desync.
- `[Authorize(Roles = "Instructor")]` works with Identity out of the box.
- JWT claims are automatically populated by Identity from roles.
- Less code, fewer chances for errors.

**Alternatives:**
- Only an enum on User, no Identity roles — we lose `[Authorize(Roles=...)]` and built-in support, requiring custom authorization handlers.
- Hybrid: enum + Identity, synchronized via domain method — previous recommendation, rejected because duplication even for 3 values isn't worth it. If a 4th role is added, we'd need to change it in two places.

---

## ADR-BACK-AUTH-005: JWT secret — placeholder in base + dev-secret in Development + env var in production

**Context:** the JWT signing secret must never be committed to git, yet a new developer running
`dotnet run` for the first time still needs the API to boot without any manual setup step.

**Decision:** `appsettings.json` contains `Jwt.Secret = ""` (placeholder, startup validation fails if empty). `appsettings.Development.json` overrides it with a static random string (>32 bytes). In production, the value is passed via the environment variable `JWT__Secret` (double underscore = nested config key in .NET configuration).

**Why:**
- Developer runs `dotnet run` and the JWT config does not fail on startup — no extra steps for a secret that only exists to make Development boot. (The database is a separate matter: it is migrated by `Learnix.DbMigrator`, never by the API — see [ADR-BACK-MIGR-001](MIGRATIONS.md).)
- `appsettings.Development.json` never goes into production build — low leak risk.
- Production secret never touches disk or git — only runtime env var (Azure Key Vault → App Service config → env var).
- Explicit check `string.IsNullOrWhiteSpace(jwtOptions.Secret)` in `AuthenticationExtensions.AddLearnixAuthentication` (API layer, where the JWT bearer pipeline is wired) — fail fast, better to crash on startup than issue tokens signed with an empty key.

**Alternatives:**
- Always via env var (including dev) — every new developer has to manually configure `.env` or user-secrets before first run. Friction in onboarding.
- User Secrets (`dotnet user-secrets`) for dev — canonical MS approach, but hides the secret in a separate location complicating "where did this value come from" debugging.
- Hardcoded fallback in code (`?? "default-dev-secret"`) — dangerous, easily missed in production builds.

**Consequences:**
- `appsettings.json`: `Jwt` section with empty `Secret` **and** empty `RefreshTokenSecret` — there are two
  secrets, not one. The second is the HMAC pepper for refresh-token hashes (ADR-BACK-AUTH-017) and is
  provisioned in production as `PROD_JWT_REFRESH_SECRET`. Configuring one and forgetting the other is the
  easy mistake here.
- `appsettings.Development.json`: overrides both with random strings.
- `AddLearnixAuthentication`: explicit presence check, throwing `InvalidOperationException` if empty.

---

## ADR-BACK-AUTH-006: Decomposition of Identity service into three roles based on SRP

**Context:** Application handlers need to register users, validate credentials and issue tokens, but
`UserManager<User>` is an Infrastructure concern they cannot depend on directly.

**Decision:** Application handlers never see `UserManager<User>` directly — it depends on `IUserStore` →
EF Core, an Infrastructure concern, and calling it from a handler would violate the dependency rule. What
sits between them is not one interface but three:
- `IUserRegistrationService` — registration + email confirmation (CRUD life-cycle of user).
- `IUserAuthenticationService` — credentials validation + fetching info to build the token.
- `ITokenService` — JWT generation + refresh token generation + hashing (pure function with no DB or Identity knowledge).

All three live in `Auth/Abstractions/` (ARCHITECTURE.md ADR-BACK-ARCH-009). Implementations — in `Infrastructure/Identity/`.

**Why three interfaces, and not the single one this replaced:** the boundary itself — Application says
"register / confirm email", Infrastructure decides how — was right from the start and is unchanged here.
What didn't hold up was putting the whole boundary behind one `IIdentityService`: a single fat contract
changes for any reason at all, so swapping the Identity provider and swapping JWT for PASETO both meant
touching the same file, and every Login-handler test mocked a service ten times larger than what the test
needed. Splitting it means each interface changes for exactly one reason: swap the Identity provider and
only `UserRegistration`/`UserAuthentication` move, swap the token format and only `TokenService` does.

**Why:**
- **Different reasons to change.** If you swap the Identity provider (for Auth0/IdentityServer) — you rewrite `UserRegistration` + `UserAuthentication`, `TokenService` is unaware. If you swap JWT for PASETO or change claims — you rewrite `TokenService`, the rest is untouched. Single Responsibility Principle in action.
- **Testability.** In unit tests for the Login handler, you mock three lightweight interfaces instead of one fat interface.
- **Handler readability.** `LoginCommandHandler` explicitly shows orchestration: validate → generate token pair → persist refresh → save. Each step is a separate dependency.

**Alternatives:**
- Single `IIdentityService` with all methods (the design this replaced) — simpler, fewer files, but a fat contract that changes for any reason, which is exactly what motivated the split.
- `IUserAuthenticationService.LoginAsync` immediately returning a JWT — mixes credentials validation with token generation, two distinct concerns in one method.
- Direct `UserManager` calls from handlers, no interface at all — simpler, but reopens the dependency-rule violation an interface exists to close.
- A separate "Auth module" wrapping Identity as one unit — overengineering for what three focused interfaces already solve.

**Consequences:**
- Each interface gets its own DI registration in `AuthModule` (composed into `AddInfrastructure`).
- Old `IdentityService.cs` removed, replaced by `UserRegistrationService.cs` + `UserAuthenticationService.cs` + `JwtTokenService.cs`.
- Old handlers (Register, ConfirmEmail, ResendConfirmationEmail) updated — constructor parameter changed from `IIdentityService` to `IUserRegistrationService`.

---

## ADR-BACK-AUTH-007: Refresh token rotation with replay-attack protection

**Context:** ADR-BACK-AUTH-001 gave the refresh token a 7-day lifetime; a stolen token needs to be
*detected*, not just outlast its expiry unnoticed.

**Decision:** On every successful `/api/auth/refresh` — the old refresh token is revoked (not deleted), a new one is created and returned. If a request arrives with an **already revoked** token — this indicates a compromise: all active tokens for the user are forcibly revoked, the user is logged out from all devices, and the incident is logged as a warning with the UserId.

Refresh tokens are stored in PostgreSQL as an **HMAC-SHA256** hash keyed with a pepper (`TokenHash`, unique index) — see ADR-BACK-AUTH-017, which superseded the plain SHA-256 this ADR originally described. The plain token exists only in the client's HttpOnly cookie. DB leak ≠ session compromise.

**The cookie** (`learnix_refresh`, `Path=/api/auth`) is `HttpOnly` always, and:

| | Development | Production |
|---|---|---|
| `Secure` | `false` | `true` |
| `SameSite` | `Strict` | **`None`** |

`None` in production is not a weakening by choice — the SPA (Static Web Apps) and the API (Container Apps) are on different registrable domains, so the refresh cookie *is* cross-site, and a browser silently drops a cross-site cookie that is not `SameSite=None; Secure`. The CSRF exposure that `SameSite` would otherwise cover is carried instead by: the cookie being `Path`-scoped to `/api/auth`, a strict CORS allow-list of the one frontend origin, and rotation with replay detection — a forged refresh from another origin burns the token and logs everyone out, which is loud rather than silent.

The Controller handles reading/writing the cookie; handlers operate on raw strings — the Application layer knows nothing about HTTP.

**Why:**
- Rotation minimizes the compromise window — a token lives for exactly one request.
- Replay protection catches the "token was stolen, both attacker and user are using it" scenario — the first to refresh gets a new one, the second comes with the old (revoked) one → everyone gets logged out.
- Hashing in DB — protection against database dump leaks.
- Path-restricted cookie is not sent with every API request, only to auth endpoints — less exposure.

**Alternatives:**
- Refresh without rotation (single long-lived token) — simpler, but loses replay-detection.
- Refresh tokens in Redis — faster, but loses durability (Redis restart = all users logged out).
- JWT as refresh token too — symmetric with access tokens, but loses central revocation control (JWT cannot be "taken back").

**Consequences:**
- `RefreshToken` entity with `TokenHash`, `ExpiresAt`, `IsRevoked`, `RevokedAt`.
- `IRefreshTokenRepository` + specifications `RefreshTokenByHashSpecification`, `ActiveRefreshTokensByUserSpecification`.
- `RefreshTokenCleanupHostedService` (B-11.5) — background task cleaning up tokens older than `ExpiresAt + 7 days` every 24h.
- Controller `AuthController` manages cookies (`SetRefreshTokenCookie`, `ClearRefreshTokenCookie`) — handlers do not.

---

## ADR-BACK-AUTH-008: JWT claims — standard OIDC + custom for roles

**Context:** the access token needs to carry the identity data the frontend and API both consume — who
the user is, their role, their display name — without a database round-trip on every request.

**Decision:** Access token contains:
- `sub` — User Id (Guid)
- `email` — User email
- `jti` — unique token id (for future tracing/blacklist)
- `given_name` — FirstName
- `family_name` — LastName
- `name` — `"{FirstName} {LastName}"` (full name for display)
- `role` — repeated claim for each user role

`MapInboundClaims = false` in `AddJwtBearer` — so that in our API code we see claim names exactly as they are in the JWT (not converted to `ClaimTypes.NameIdentifier`, etc.). `NameClaimType = "name"`, `RoleClaimType = "role"` — so that `User.Identity.Name` and `[Authorize(Roles = "Instructor")]` work with our custom claim names.

**Why:**
- Standard OIDC claims (`sub`, `email`, `given_name`, `family_name`, `name`) — frontend or third-party systems expecting OIDC get what they expect.
- Separate `given_name` + `family_name` + composite `name` — frontend can grab any field without extra parsing.
- `role` as a repeated claim — standard Identity mechanism, works with `[Authorize(Roles = ...)]` by default.
- `MapInboundClaims = false` — consistency: what is in JWT == what we see in code. Debugging is easier.

**Alternatives:**
- Only `name` without splitting — frontend has to parse "First Last", breaks on names with spaces/multiples.
- Custom short claim names (`uid`, `r`) to reduce token size — minimal savings, loss of compatibility with OIDC tooling.
- `ClaimTypes.*` URI-based claim names (.NET default) — multi-kilobyte tokens, poor readability.

**Consequences:**
- `JwtTokenService.GenerateAccessToken` accepts `firstName, lastName` (not just `firstName` like in the first iteration).
- `UserAuthenticationInfo` contains both names.
- `AddJwtBearer` configured with `MapInboundClaims = false`, `NameClaimType`, `RoleClaimType`.

---

## ADR-BACK-AUTH-009: Separation of `AuthenticationError` (401) and `ForbiddenError` (403)

**Context:** the API needs to tell "you're not logged in" apart from "you're logged in but not allowed",
and one error type was doing both jobs.

**Decision:** Created a separate typed error `AuthenticationError : Error` for 401 Unauthorized.
`ForbiddenError` now semantically maps to 403 Forbidden — "authenticated, but lacks permissions".

- `AuthenticationError` — invalid credentials, expired/replay refresh token, missing/invalid access token, locked out, unconfirmed email during login. Maps to 401.
- `ForbiddenError` — user is authenticated but doesn't have rights for the operation (e.g. Student trying to edit someone else's course). Maps to 403.

**Why:**
- HTTP 401 and 403 are semantically different. RFC 9110 clearly separates them: 401 = "needs authentication", 403 = "authentication exists, but doesn't grant access".
- Previous implementation mapped `ForbiddenError` to 401 in the controller — it worked but confused readers (type name → 403, mapping → 401).
- Distinct types allow the `result.ToActionResult()` extension to work unambiguously without overrides at the action level.

**Alternatives:**
- Keep single `ForbiddenError` and map to different codes depending on context — magic in mapping, service doesn't know which code its error returns.
- Error codes (enum) in a single type — less expressive, loses compile-time checks.

**Consequences:**
- `ResultExtensions.ToActionResult` has separate branches for both types.
- Existing handlers (`UserAuthenticationService`, `RefreshTokenCommandHandler`) migrated to `AuthenticationError`.
- Future role-based authorization checks will use `ForbiddenError`.

---

## ADR-BACK-AUTH-010: Google OAuth via Google Identity Services (ID token) instead of OAuth code flow

**Context:** the platform needs Google sign-in, and the classic OAuth Authorization Code flow expects a
backend redirect endpoint and a client secret an SPA has no safe place to keep.

**Decision:** Frontend obtains a Google ID token via Google Identity Services (GIS) SDK directly in the browser. Backend receives the token via `POST /api/auth/google`, validates it via `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync`), and issues its own JWT+refresh tokens. Authorization Code flow with redirect_uri on the backend and Client Secret is **not used**.

**Why:**
- GIS — sanctioned Google approach for SPAs since 2022+. Simpler, fewer moving parts.
- Client Secret is not needed — an ID token is a self-contained JWT signed by Google's private key, the backend validates it using the public key from JWKS. Secret is only needed to exchange authorization codes.
- No redirect endpoint on backend → no extra machinery for callback, state parameter, CSRF protection on callback.
- ID token already contains `email`, `email_verified`, `given_name`, `family_name`, `sub` — everything we need for find-or-create. No extra requests to Google's userinfo endpoint are made.

**Alternatives:**
- **Authorization Code flow** — classic, "looks more standard" on interviews, but for SPAs it's an anti-pattern in 2026. Requires Client Secret, redirect endpoint, code → tokens exchange.
- **Implicit flow** — deprecated by Google, not an option.

**Consequences:**
- `GoogleOptions.ClientId` is the only thing to configure. `ClientId` is public (exposed in front-end code), not a secret.
- Endpoint `POST /api/auth/google` accepts `{ idToken }` → validates → issues a token pair (same `LoginResponse` as regular login).
- If Google ever deprecates GIS — we will have to rewrite to Authorization Code flow. Low risk: GIS is their strategic direction.

---

## ADR-BACK-AUTH-011: `GoogleId` as denormalized field on User instead of `AspNetUserLogins`

**Context:** a Google-authenticated user needs to be found by their Google identity on every login, and
Identity's own login table is built to support more providers than this platform has.

**Decision:** External provider linkage is stored as `User.GoogleId` (nullable `string?`), not via the Identity table `AspNetUserLogins` / `UserManager.AddLoginAsync`.

**Why:**
- In v1 Learnix, there is only one external provider (Google). `AspNetUserLogins` is a table for N providers `(Provider, ProviderKey)`. For just one — overhead without benefits.
- `WHERE GoogleId = ?` — single simple lookup without joining `AspNetUserLogins`.
- Less EF configuration, fewer moving parts in Identity schema.

**Alternatives:**
- **`AspNetUserLogins` via `UserManager.AddLoginAsync`** — canonical Identity path. Pros: scales to N providers with zero code changes (GitHub, Microsoft). Cons: join on every Google login lookup.
- **Hybrid: `GoogleId` for fast lookup + save in `AspNetUserLogins`** — duplication, desync possible.

**Consequences:**
- Adding a second external provider (GitHub, Microsoft) means a migration from `string? GoogleId` → `AspNetUserLogins`-based flow. Significant work: new schema migration, data transfer, rewriting `FindOrCreateGoogleUserAsync` to polymorphic `FindOrCreateExternalUserAsync`.
- Limited to a single-provider scenario — documented as a deliberate tradeoff.

**Future work:** when adding a second provider — refactor to `UserManager.AddLoginAsync` + `FindByLoginAsync`. Added as a `B-XX` task in TODO (outside v1).

---

## ADR-BACK-AUTH-012: Rate limiting — in-memory FixedWindow, `AuthStrict` partitioned by IP **and path**

**Context:** credential endpoints — login, registration, password reset, email confirmation — are the
ones worth brute-forcing, and need a request budget before an attacker gets meaningful attempts.

**Decision:** anything that accepts or issues a credential — registration, login (password or Google), the password-reset pair, and the email-confirmation pair — runs under the `AuthStrict` policy of the built-in `Microsoft.AspNetCore.RateLimiting`: **5 requests per 15 minutes**, FixedWindow, `QueueLimit = 0`. Refresh and logout are deliberately unlimited. Over the limit → 429 `ProblemDetails` + `Retry-After`.

The partition key is **`{ip}_{path}`**, not the IP alone: a user fumbling their password does not spend the budget they need to request a reset. One endpoint's brute-force protection must not lock them out of another.

`AuthStrict` is not the only policy any more. The rest guard *cost and abuse after login*, where an identity exists to partition by — so they partition **per authenticated user**, not per IP: the AI assistant and the course tutor (separate budgets, so browsing cannot spend the tutor's), test attempts, payments, uploads and chat messages. The policies live in `RateLimitPolicies`; which endpoint carries which one is in [`ENDPOINTS.md`](../../ENDPOINTS.md), generated from the `[EnableRateLimiting]` attributes and checked in CI.

**Why:**
- `Microsoft.AspNetCore.RateLimiting` is built into .NET 8, zero extra NuGets, supported by Microsoft. AspNetCoreRateLimit is legacy from .NET Core 2 days.
- FixedWindow — most transparent for users ("5 attempts per 15 min, then reset"). SlidingWindow and TokenBucket offer no benefits for sensitive auth where we need a **strict cap**, not a smooth rate.
- `QueueLimit = 0` — a user exceeding the limit immediately gets 429, without hanging in a queue.
- Refresh without limits — a legitimate client with 3 tabs might trigger 3 simultaneous refreshes on wake-from-sleep; a strict limit would cause false positives with no security benefit (replay detection already works in `RefreshTokenCommandHandler` via ADR-BACK-AUTH-007).
- Per-IP partitioning (not per IP+email) — simpler, sufficient for a portfolio. Per-user partitioning requires identifying the user — but rate limiting applies **before** auth, when the user doesn't exist yet.

**Alternatives:**
- **AspNetCoreRateLimit (NuGet)** — legacy, more code, less support.
- **Redis-backed distributed rate limiter** — correct for scale-out. Outside scope for v1: monolithic deployment on a single Container App instance → in-memory is enough. Add when migrating to multi-instance (Phase 10+).
- **Per IP+email for login** — protects a specific account from brute force during distributed IP attacks. Trade-off: more complex, requires a custom partitioning key (extracting email from body). Not justified for v1.

**Consequences:**
- In-memory counters — **counters drift on scale-out**. An attacker gets 5×N attempts across N instances. A deliberate trade-off while the deployment is single-instance; the fix, when it matters, is a Redis-backed limiter.
- Behind a proxy, `RemoteIpAddress` is the proxy's, which would collapse every client into one partition and turn the limiter into a global counter. `app.UseForwardedHeaders()` is configured (`Program.cs`), so this is handled — the remaining question is *which* proxies to trust, and that is [FORWARDED_HEADERS.md](../operations/FORWARDED_HEADERS.md).

---

## ADR-BACK-AUTH-013: Authorization checks live in handlers, not controllers

> **Status:** Narrowed by ADR-BACK-AUTH-018. The principle holds for **resource-based** authorization —
> the owner check this record was written around, and which everything below still describes correctly.
> The **coarse role check** it also admitted has moved to the endpoint attribute. That half was never
> load-bearing: the attribute it duplicated was already on the route and already ran first, so the
> handler copy could not execute. ADR-BACK-AUTH-014 had in fact already read this record the narrower
> way ("reserves handler-level auth checks for resource-based (owner) decisions") — 018 makes the text
> match the reading.

**Context:** a mutation needs to know whether the current user actually owns the resource they're
changing — a decision only answerable once the entity is loaded, which is the handler's job, not the
route's.

**Decision:** Checks for "can the current user perform this operation on this resource" (owner check, role check) are performed inside command/query handlers via `ICurrentUserService`. The Controller does not take this responsibility — it only handles HTTP concerns (read body, return ToActionResult).

**Why:**
- The controller does not know `course.InstructorId` without fetching it via a repository. If the controller fetches it — it effectively performs part of the handler's job → SRP violation.
- ASP.NET `[Authorize(Policy = ...)]` works well for static rules (role in claims, claim value). Owner checks require fetching the resource → resource-based authorization → dynamic → natural place is the handler.
- Handler returns `Result.Fail(new ForbiddenError(...))` → `ToActionResult()` maps to 403. Aligned with existing pipeline (ARCHITECTURE.md ADR-BACK-ARCH-002, ADR-BACK-ARCH-004).
- One place to look — all business rules are visible in a single layer.

**Alternatives:**
- Authorization in controller via custom `IAuthorizationRequirement + AuthorizationHandler` — official ASP.NET approach for resource-based auth. Rejected: adds a layer of indirection with no benefit for a solo project with a single owner-check type (`InstructorId`).
- Authorization in domain entity method (`course.UpdateDetails(..., requestingUserId)`) — mixes identity knowledge with entity business logic, violates SRP.

**Consequences:**
- Every mutating handler makes two checks: authenticated (401) and owner-or-admin (403).
- **They are no longer copy-pasted.** Course structure mutations inherit `CourseCommandHandler`, which runs
  both before the handler body executes (ADR-BACK-ARCH-019) — the extraction this ADR once anticipated as
  "a cosmetic refactor, non-blocker" happened, and it is not cosmetic: it is what makes a forgotten
  ownership check impossible across every course-structure handler, instead of a rule each one has to
  remember on its own.
- One extra fetch on a mutation, for an entity the handler was going to load anyway.

---

## ADR-BACK-AUTH-014: Email confirmation soft restriction via ASP.NET Core authorization policy

**Context:** an unconfirmed email shouldn't stop someone from exploring the platform, but some actions
commit them to it — or to other people — in a way that isn't safe to allow before they've confirmed.

**Decision:** After registration, the user is automatically logged in, but the email remains unconfirmed. A persistent banner in the UI reminds them to confirm their email. Write-actions with real platform impact are protected by a named policy `EmailConfirmed`, which checks the `email_verified` claim in the JWT. Unconfirmed users can freely browse the catalog and their profile; specific endpoints return 403 when the policy is not met.

**What gets gated — the rule, not a list:** an action is behind the policy when it *commits the user to
the platform or to other people*. Three kinds qualify:

1. **It starts an entitlement.** Enrolling — free or paid — is the gate everything downstream inherits:
   progress, test attempts and certificates all cascade from an `Enrollment`, so gating them separately
   would be gating the same decision twice.
2. **It publishes something under a real identity.** Writing or editing a review.
3. **It reaches another human.** Opening a conversation or sending a message; and applying to be an
   instructor, which lands on an admin's desk.

**What stays open:** reading. Catalog, course pages, reviews, one's own profile, the AI chat. An
unconfirmed user can look around and decide the platform is worth confirming an email for.

Which endpoints that currently amounts to is not recorded here — it is in
[`ENDPOINTS.md`](../../ENDPOINTS.md), where the `Auth` column reads `Authenticated + EmailConfirmed`,
generated from the controllers and checked against them in CI. A gate added or removed in code shows up
there whether or not anyone remembers to update prose.

**Why:**
- **Controller-level concern, not domain concern.** "Is the user's identity confirmed?" is an authentication/authorization question, not business logic. The natural place is an `[Authorize]` attribute (same level as role checks), not inside handlers — aligns with ADR-BACK-AUTH-013, which reserves handler-level auth checks for resource-based (owner) decisions.
- **One mechanism, not one per handler.** A single named policy decorates every gated endpoint. The alternative (checking `ICurrentUserService.IsEmailConfirmed` in every handler) scatters auth logic across the Application layer and complicates auditing.
- **Soft restriction (no hard block).** Hard-blocking login/access until email confirmation causes high abandonment rates. Allowing exploration before confirmation is an industry standard (Slack, GitHub, Vercel).
- **JWT claim = zero extra DB queries.** The claim `email_verified: "true"/"false"` is set during login/registration and lives in the token — no extra queries per request. Frontend reads the same claim to display the banner.

**Alternatives:**
- **Hard-block login** — maximum security, but high cost: UX degrades, abandonment rises. Rejected.
- **Checking `IsEmailConfirmed` in every handler** — auth concerns bleed into Application layer, violating the "auth at the gate" principle. Complicates auditing: checks scattered across 7 handlers. Rejected.
- **Middleware checking specific paths** — fragile: string path matching breaks during route refactoring. Rejected.
- **Custom attribute `[RequireEmailConfirmed]`** — equivalent to named policy, but less standard; ASP.NET Core authorization policies are the proper mechanism for named authorization rules. Rejected.

**Consequences:**
- `JwtTokenService.GenerateAccessToken` adds `email_verified: "true"/"false"` claim (string, consistent with OIDC standard).
- `ICurrentUserService` expanded: `bool IsEmailConfirmed`.
- `CurrentUserService` reads `email_verified` claim from `ClaimsPrincipal`.
- New named policy `EmailConfirmed` registered in `AuthenticationExtensions.AddLearnixAuthentication` (`Learnix.API`).
- Every gated endpoint receives `[Authorize(Policy = "EmailConfirmed")]` on top of its existing `[Authorize]`; which ones currently qualify is `ENDPOINTS.md`'s answer, not this file's.
- Frontend: `isEmailConfirmed: boolean` added to auth store; persistent banner displayed if `false`; on 403 from gated endpoint — a toast localized from the `code`, pointing the user at the same banner rather than duplicating its resend action in a second UI.

---

## Request Lifecycle (Authorization Process)

Simulation of a request journey from the client to business logic execution:

1. **Client (Frontend):** 
   Sends a request to a protected endpoint, adding `Authorization: Bearer <access_token>` in the headers. If it's a refresh token request, it automatically sends the HttpOnly cookie `learnix_refresh` via the browser (credentials: 'include').

2. **Middleware (ASP.NET Core JwtBearer):** 
   The request hits `JwtBearerMiddleware`. The token is validated: checks signature (using `Jwt.Secret`), expiration (`exp`), and integrity. `ClaimsPrincipal` is constructed from JWT claims and assigned to `HttpContext.User`. If the token is invalid or expired — middleware returns `401 Unauthorized` and the request goes no further.

3. **Authorization middleware (`UseAuthorization`) — before the controller, not inside it:**
   The endpoint's `[Authorize]` metadata is evaluated here, against the `ClaimsPrincipal` from step 2 —
   **before model binding and before the action method exists**. Authentication (`[Authorize]`), role
   membership (`[Authorize(Roles = …)]`) and named policies (`[Authorize(Policy = "EmailConfirmed")]`)
   are all decided at this point, and the request never reaches MediatR. On a **403**,
   `ProblemDetailsAuthorizationResultHandler` writes an RFC-7807 body carrying a machine-readable `code`
   (`insufficient_role`, `email_not_confirmed`) so the client can tell the two apart; a **401** stays a
   bare challenge owned by the JWT bearer scheme. **This step is why a role check restated in a handler
   is unreachable** (ADR-BACK-AUTH-018).

4. **Controller:**
   Reads the request payload and dispatches a command/query via MediatR (`sender.Send(...)`). It takes no
   authorization decisions of its own.

5. **Application Handler (Business Logic):**
   The handler injects `ICurrentUserService` (which reads `HttpContext.User` under the hood) and decides
   only what step 3 could not — questions that need resource state or that are not a gate at all:
   - Narrows `Guid?` to `Guid`. `if (currentUser.UserId is null) return Result.Fail(new AuthenticationError());`
     This is a nullability contract, not a gate: the gate was step 3.
   - Performs the owner check: whether the course belongs to the current `InstructorId`.
     `if (!course.IsOwnerOrAdmin(currentUser)) return Result.Fail(new ForbiddenError());`
   
6. **Service Layer (Infrastructure/Identity):** 
   If it's a login or registration request, the handler calls `IUserAuthenticationService` or `IUserRegistrationService` to validate passwords or generate new tokens (which in turn utilize `UserManager` from ASP.NET Core Identity).

---

## ADR-BACK-AUTH-016: 6-Digit OTP for Email Confirmation instead of Magic Link

**Context:** email confirmation needs to work across devices — a magic link opened on a different tab or
phone than the one registration happened on leaves the original tab stuck waiting.

**Decision:** The email confirmation flow was refactored to use a 6-digit Time-based One-Time Password (TOTP) valid for 3 minutes, sent via email, rather than a traditional "magic link". Upon successful validation of the code, the API immediately returns an `AuthResponse` (Access and Refresh tokens), allowing seamless automatic login.

**Why:**
- **Context Preservation (UX):** With magic links, the user clicks the link on their phone or a different browser tab, confirming the email there, but leaving the original registration tab in a disconnected state (requiring them to manually log in again).
- **Auto-Login:** By returning tokens upon successful `/api/auth/confirm-email`, the frontend can automatically log the user in without requiring them to re-enter their credentials.
- **Stateless & Scalable:** By leveraging ASP.NET Core Identity's `TotpSecurityStampBasedTokenProvider` (which uses RFC 6238 internally), we avoid storing temporary codes in the database. The validation is performed mathematically based on the shared secret (the user's Security Stamp) and the current time window.

**Alternatives:**
- **Magic Link (Previous Implementation):** Sent a long base64-encoded string via email. Required a dedicated `verify-email` route expecting URL parameters. It was stateless but broke user context across devices and tabs, leading to poor UX.
- **Stateful 6-Digit Code in DB (`UserVerificationTokens` table):** A common approach where a random 6-digit string is generated and stored in a table with an `ExpiresAt` column.
    - *Pros:* 100% control over the lifecycle. Ability to easily track retry attempts, explicitly invalidate a code after use, or enforce a strict custom expiration time (e.g. 15 minutes).
    - *Cons:* Adds database bloat. Requires schema migrations. Requires a background cleanup job for expired tokens. Too complex for a fast, elegant solution in a pet project.
- **Tracking failures via `AccessFailedCount`:** Attempting to reuse the Identity User's `AccessFailedCount` to limit OTP retry attempts.
    - *Rejected because:* This is a mixing of responsibilities (SRP violation). `AccessFailedCount` is specifically meant for login brute-force protection. Mixing it with verification attempts causes architectural debt and confusion.

**Consequences:**
- The built-in `TokenOptions.DefaultEmailProvider` (which is a `TotpSecurityStampBasedTokenProvider`) is registered for `EmailConfirmationTokenProvider` in `DependencyInjection.cs`. It natively generates a 6-digit code valid for ~3 minutes.
- A constant `EmailConfirmationTokenExpirationMinutes = 3` is added to `AuthValidationConstants.cs` to explicitly document this behavior for other developers.
- `UserRegisteredDomainEventHandler` sends the raw 6-digit code to the email service without base64 encoding.
- The `ConfirmEmail` API endpoint is heavily protected against brute-force attacks by the existing `AuthStrict` rate-limiting policy (5 requests per 15 minutes per IP), making guessing a 6-digit code mathematically impossible.
- `ConfirmEmailCommandHandler` now generates and returns JWT and Refresh tokens upon successful verification (calling `ITokenService`), functioning similarly to `LoginCommandHandler`.

---

## ADR-BACK-AUTH-017: HMAC-SHA256 with Pepper for Refresh Tokens

**Context:** a leaked database of hashed refresh tokens should not let an attacker verify a separately
stolen raw token against it.

**Decision:** The hashing mechanism for Refresh Tokens was upgraded from a standard `SHA256` to `HMAC-SHA256` utilizing a globally configured Secret Key (Pepper) defined in `Jwt:RefreshTokenSecret`.

**Why:**
- While a 64-byte random string (512-bit entropy) is already mathematically immune to brute-force attacks even with standard SHA256, adding a Pepper provides absolute immunity against **offline verification**.
- In the event of a database leak, an attacker would possess the token hashes. Without the Pepper (which resides only in the application's configuration/environment variables and not in the database), the attacker cannot verify whether a stolen raw refresh token matches any hash in the database.
- Key Separation Principle: `Jwt:Secret` is used exclusively for signing Access Tokens (JWTs), while `Jwt:RefreshTokenSecret` is used exclusively for hashing Refresh Tokens. 

**Alternatives:**
- **Standard SHA256 (Previous Implementation):** Secure against brute force due to high entropy, but allows an attacker with a leaked database to verify intercepted raw tokens offline.
- **Salting (bcrypt/Argon2):** Unnecessary for machine-generated high-entropy tokens. Salts protect low-entropy secrets (like human passwords) against rainbow tables.

**Consequences:**
- `JwtOptions` requires a new configuration property `RefreshTokenSecret`.
- CI/CD pipelines and deployment documentation must include the provisioning of `PROD_JWT_REFRESH_SECRET`.
- The `HashRefreshToken` method in `JwtTokenService` now requires the instantiation of `HMACSHA256` with the provided Pepper.

---

## ADR-BACK-AUTH-018: The coarse role gate is the endpoint attribute; the handler keeps only what the attribute cannot answer

**Context:** ADR-BACK-AUTH-013 let both owner checks and coarse role checks live in handlers; the role
half turned out to be redundant — every route already enforces the same rule through its attribute first.

**Decision:** A role check whose only outcome is "in or out", and which can be answered from JWT claims
alone, lives on the endpoint as `[Authorize(Roles = …)]` and nowhere else. It is not restated inside the
handler. This narrows ADR-BACK-AUTH-013, which admitted both owner *and* role checks into handlers.

Because the attribute now carries that decision by itself, it must also answer properly: an
`IAuthorizationMiddlewareResultHandler` gives every authorization failure an RFC-7807 body with a stable
machine-readable `code`. The two halves are one decision, not two — moving the gate onto the attribute
while the attribute still replies with an empty body would trade a `ProblemDetails` for nothing.

**The criterion — what stays in the handler.** A check stays when answering it needs something the
endpoint does not have:

1. **It reads resource state.** "Is this the caller's own course" requires loading the course. The
   endpoint has claims, not rows.
2. **Its outcome is not a gate.** The role picks a branch, or decides which *sub-operation* of an
   otherwise-open endpoint is allowed, or is a business precondition whose failure is a domain conflict
   rather than a locked door ("you are already an instructor").
3. **The handler is reachable from a second dispatch path** (below).

Everything else — role in claims, single in/out outcome — is the attribute's job.

**The same test applies to `if (currentUser.UserId is null)`.** It survives only where the handler goes
on to *use* `UserId.Value`: there it is a nullability contract, narrowing `Guid?` to `Guid`, and the
`AuthenticationError` is the unreachable branch of a conversion. Where the handler never reads the id,
nothing is being narrowed and the check is a bare authentication gate — which is the attribute's job,
so both the check and the `ICurrentUserService` dependency go. Injecting a service solely to null-check
it is the same dead weight as the role check, one layer down; removing the role check is what exposes
it, so the two are found together.

**The trap this record exists to prevent:** both categories call `ICurrentUserService.IsInRole`. The
discriminator is the *question asked*, not the method called. `Course.IsOwnerOrAdmin` calls
`IsInRole(Admin)` and must never be removed — a mechanical sweep for `IsInRole` would delete it and
silently open every instructor's course to every other instructor. **This refactor cannot be executed by
grep.**

**The second dispatch path:** the attribute is the gate only for a handler reached exclusively through
its own routed endpoint. AI chat tools dispatch queries straight through `IMediator`, so the gate for
those handlers is whatever the *chat* endpoint declares — `[Authorize]`, i.e. any authenticated user —
and not the attribute sitting on some other controller. A handler reachable from a tool, a hub or a
worker keeps its own check, and the reason is recorded at the check.

**Why:**
- **Over HTTP the duplicates are dead code.** Authorization middleware runs before MVC and before
  MediatR. Every coarse role check in a handler sits behind an equal-or-stricter attribute on its route,
  so over HTTP it has never executed, and nothing it does is observable — including its message.
  **That is narrower than "it buys no depth", which an earlier draft of this record claimed.** For a
  dispatch that never passes a route — a worker, a hub, a chat tool — the handler copy *was* the only
  check, and it failed closed: `ICurrentUserService` reads `HttpContext`, so outside a request there is
  no user and the check rejects. What this decision trades away is that accidental fail-closed default,
  in exchange for one declaration instead of two. The trade is deliberate, and the criterion's third
  clause is what re-opens it.
- **The custom message it was kept for was never delivered.** The concern that justified handler-side
  checks was losing bespoke 403 text. The attribute short-circuits first, so the handler's string never
  reached a client to begin with.
- **…and it could not be shown anyway.** Those strings are hardcoded English while the client is
  localized (en/uk) under a "never hardcode UI strings" rule. Server prose is not displayable UI text.
  What the client needs from the server is a *code*; the wording is the client's to own.
- **Static rules belong where they are enforced.** On the route the rule is visible in Swagger, verified
  against the controllers by `check:endpoints` in CI, and applied before model binding. Restated in a
  handler it is visible only to whoever opens that handler.
- **It closes a live gap.** The `EmailConfirmed` policy fails with the same bodyless 403 as a role
  failure, so the client cannot tell "wrong role" from "confirm your email" — while ADR-BACK-AUTH-014
  promises a localized confirm-email toast on exactly that 403. The result handler is what makes that
  promise executable.

**Alternatives:**
- **Leave the duplicates as defence in depth.** Rejected, but not because the depth was imaginary — over
  a non-HTTP dispatch it was real (see the first *Why*). Rejected because a rule written twice by hand
  is a rule that will diverge, and because these copies read as load-bearing over HTTP, where they are
  not: reviewers trust them and their unit tests pass while proving nothing about production. Depth is
  worth having; hand-copied depth is not the way to get it. If we want it, the next alternative is how.
- **`AuthorizationBehavior` + an attribute on the command** — the approach of the widely used .NET Clean
  Architecture template (Jason Taylor's), where `[Authorize(Roles = …)]` decorates the *command* and a
  MediatR behavior enforces it. This is a coherent rival, not a worse version of this decision: it also
  keeps exactly one declaration, and it places it where transport cannot bypass it. Rejected *for now*,
  on three facts rather than on principle — HTTP is the only transport that reaches a privileged
  handler; `check:endpoints` already verifies route attributes against the controllers in CI, and can
  verify nothing about a behavior; and the route attribute is visible in Swagger and applies before
  model binding. **Change any of those facts and this decision should flip** — most plausibly by a chat
  tool, hub or worker dispatching a privileged handler, which criterion 3 is written to catch. A
  variant that derives both layers from one declaration is available if depth is later judged worth its
  machinery.
- **`UseStatusCodePages()`** — one line, gives the 403 a generic body. Rejected as insufficient: a title
  is not a discriminator, so the client still cannot separate role from email.
- **Per-endpoint custom text via endpoint metadata** — buildable on top of the result handler, rejected as
  unused: the client localizes from the code, so server-side prose would be dead weight written in a
  language half the users do not read.

**Consequences:**
- The coarse role check leaves the Application layer entirely — the rule lives on the route attribute,
  not restated by hand inside a handler. `ICurrentUserService.IsInRole` stays only where the criterion
  keeps it: resource, branching and business-rule checks, `Course.IsOwnerOrAdmin` among them.
- **A privileged command dispatched outside a request now executes instead of failing closed.** Before,
  `currentUser.UserId` was null off-request and the handler rejected; that check is gone. Nothing
  dispatches these commands outside a controller today — the outbox carries its own message types and
  the chat tools are read-only queries — which is why this is accepted rather than mitigated. It is the
  price of the decision, and criterion 3 is the tripwire.
- A handler left injecting `ICurrentUserService` for nothing but the null-check, once its role check was
  gone, loses the dependency along with the check — its constructor shrinks, and the substitute
  disappears from its tests.
- **Their unit tests go with them.** Tests asserting "handler returns Forbidden when the caller lacks the
  role" assert a path production never reaches. They are deleted, not rehomed: the rule now lives on the
  route, and `check:endpoints` is what verifies it.
- Before a check is removed, its handler must be confirmed to sit behind an equal-or-stricter attribute on
  **every** route that reaches it — a handler with two routes has two attributes to verify.
- A new `ProblemDetailsAuthorizationResultHandler` is registered in the API composition root; a **403**
  from an attribute gains an RFC-7807 body carrying a `code` extension. Handler-produced failures already
  carry `ProblemDetails` via `ResultExtensions`, so both paths now agree in shape.
- **401 is deliberately left alone** — the default handler delegates the challenge to the JWT bearer
  scheme, which owns `WWW-Authenticate` and its token-expiry description. A 401 also carries one meaning
  only, and the client's refresh flow branches on the status alone, so a body would buy nothing and
  reimplementing the challenge would risk the header.
- The client branches on `code` and takes wording from i18n. `getErrorMessage`'s fallback to
  `error.message` stops surfacing "Request failed with status code 403" to users.
- `ForbiddenError` keeps its place in the pipeline for resource failures; ADR-BACK-AUTH-009 is unaffected.
