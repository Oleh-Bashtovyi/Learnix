# Learnix — ADR: Authentication & Authorization

## Endpoints summary

| HTTP Method | Endpoint | Description | Rate Limit | Auth Required |
|---|---|---|---|---|
| `POST` | `/api/auth/register` | Register new user | Strict (5/15min) | No |
| `POST` | `/api/auth/login` | Login (returns JWT + Refresh token) | Strict (5/15min) | No |
| `POST` | `/api/auth/google` | Login via Google ID Token | Strict (5/15min) | No |
| `POST` | `/api/auth/refresh` | Get new token pair using Refresh cookie | None | No |
| `POST` | `/api/auth/logout` | Logout and invalidate Refresh token | None | No |
| `POST` | `/api/auth/forgot-password` | Request password reset | Strict (5/15min) | No |
| `POST` | `/api/auth/reset-password` | Set new password | Strict (5/15min) | No |
| `POST` | `/api/auth/resend-confirmation`| Resend email confirmation | Strict (5/15min) | No |
| `POST` | `/api/auth/confirm-email` | Confirm email via 6-digit OTP (returns JWT + Refresh token) | Strict (5/15min) | No |

---

## ADR-BACK-AUTH-001: JWT (short-lived) + Refresh Token (long-lived, HttpOnly cookie)

**Decision:** Authentication via token pair:
- **Access token (JWT):** 15 minutes, passed in `Authorization: Bearer` header
- **Refresh token:** 7 days, stored in HttpOnly + Secure + SameSite=Strict cookie

**Why:**
- A short-lived JWT minimizes the compromise window — even if stolen, it only lives for 15 minutes.
- HttpOnly cookie protects the refresh token from XSS (JavaScript has no access).
- Rotation: every refresh issues a new pair (access + refresh), the old refresh token is invalidated.
- Hashing the refresh token in the DB (SHA-256) — even if the DB leaks, tokens aren't compromised.

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

## ADR-BACK-AUTH-004: IIdentityService as an abstraction over UserManager

> **Status:** Superseded by ADR-BACK-AUTH-006 (decomposition into three interfaces). The original principle "Application doesn't know about UserManager" remains valid, but is implemented via three separate interfaces instead of a single `IIdentityService`. This ADR remains for historical context.

**Decision:** The `IIdentityService` interface lives in Application, while `IdentityService` implementation is in Infrastructure. Application handlers do not know about `UserManager<User>` — they only call `IIdentityService`.

**Why:**
- `UserManager<User>` depends on `IUserStore` → EF Core → this is an Infrastructure concern.
- Directly calling `UserManager` from a handler in Application violates Clean Architecture (Application depends on Infrastructure via MS.AspNetCore.Identity.EntityFrameworkCore).
- An interface provides a clear boundary: Application says "register / confirm email", Infrastructure knows how exactly (via Identity or otherwise).

**Alternatives:**
- Direct `UserManager` call from handler — simpler, but violates the dependency rule.
- Wrap Identity in a separate "Auth module" — overengineering for a single service.

**Consequences:**
- All auth-related handlers call `IIdentityService`, not `UserManager` directly.
- Testing handlers — we mock `IIdentityService`, not Identity infrastructure.

---

## ADR-BACK-AUTH-005: JWT secret — placeholder in base + dev-secret in Development + env var in production

**Decision:** `appsettings.json` contains `Jwt.Secret = ""` (placeholder, startup validation fails if empty). `appsettings.Development.json` overrides it with a static random string (>32 bytes). In production, the value is passed via the environment variable `JWT__Secret` (double underscore = nested config key in .NET configuration).

**Why:**
- Developer runs `dotnet run` without extra steps — DB migrates automatically (DECISIONS_INFRA.md ADR-012), JWT doesn't fail on startup.
- `appsettings.Development.json` never goes into production build — low leak risk.
- Production secret never touches disk or git — only runtime env var (Azure Key Vault → App Service config → env var).
- Explicit check `string.IsNullOrWhiteSpace(jwtSettings.Secret)` in `AddInfrastructure` — fail fast, better to crash on startup than issue tokens signed with an empty key.

**Alternatives:**
- Always via env var (including dev) — every new developer has to manually configure `.env` or user-secrets before first run. Friction in onboarding.
- User Secrets (`dotnet user-secrets`) for dev — canonical MS approach, but hides the secret in a separate location complicating "where did this value come from" debugging.
- Hardcoded fallback in code (`?? "default-dev-secret"`) — dangerous, easily missed in production builds.

**Consequences:**
- `appsettings.json`: `Jwt` section with empty `Secret`.
- `appsettings.Development.json`: override `Jwt.Secret` with a random string.
- `.env.example`: line `JWT__Secret=<generate 64+ char secret>` with a "production only" comment.
- `AddInfrastructure`: explicit check for secret presence throwing `InvalidOperationException` if empty.

---

## ADR-BACK-AUTH-006: Decomposition of Identity service into three roles based on SRP

> **Supersedes:** ADR-BACK-AUTH-004 (one IIdentityService → three interfaces)

**Decision:** Instead of a single `IIdentityService` — three interfaces:
- `IUserRegistrationService` — registration + email confirmation (CRUD life-cycle of user).
- `IUserAuthenticationService` — credentials validation + fetching info to build the token.
- `ITokenService` — JWT generation + refresh token generation + hashing (pure function with no DB or Identity knowledge).

All three live in `Auth/Abstractions/` (DECISIONS_ARCHITECTURE.md ADR-009). Implementations — in `Infrastructure/Identity/`.

**Why:**
- **Different reasons to change.** If you swap the Identity provider (for Auth0/IdentityServer) — you rewrite `UserRegistration` + `UserAuthentication`, `TokenService` is unaware. If you swap JWT for PASETO or change claims — you rewrite `TokenService`, the rest is untouched. Single Responsibility Principle in action.
- **Testability.** In unit tests for the Login handler, you mock three lightweight interfaces instead of one fat interface.
- **Handler readability.** `LoginCommandHandler` explicitly shows orchestration: validate → generate token pair → persist refresh → save. Each step is a separate dependency.

**Alternatives:**
- Single `IIdentityService` with all methods — simpler, fewer files, but a fat contract that changes for any reason.
- `IUserAuthenticationService.LoginAsync` immediately returning a JWT — mixes credentials validation with token generation, two distinct concerns in one method.

**Consequences:**
- 3 separate DI registrations in `AddInfrastructure`.
- Old `IdentityService.cs` removed, replaced by `UserRegistrationService.cs` + `UserAuthenticationService.cs` + `JwtTokenService.cs`.
- Old handlers (Register, ConfirmEmail, ResendConfirmationEmail) updated — constructor parameter changed from `IIdentityService` to `IUserRegistrationService`.

---

## ADR-BACK-AUTH-007: Refresh token rotation with replay-attack protection

**Decision:** On every successful `/api/auth/refresh` — the old refresh token is revoked (not deleted), a new one is created and returned. If a request arrives with an **already revoked** token — this indicates a compromise: all active tokens for the user are forcibly revoked, the user is logged out from all devices, and the incident is logged as a warning with the UserId.

Refresh tokens are stored in PostgreSQL as a SHA-256 hash (`TokenHash`, unique index). The plain token exists only in the client's HttpOnly cookie. DB leak ≠ session compromise.

The refresh token is passed via HttpOnly + Secure + SameSite=Strict cookie with `Path = "/api/auth"`. The Controller handles reading/writing the cookie; handlers operate on raw strings — Application layer knows nothing about HTTP.

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
- `GoogleSettings.ClientId` is the only thing to configure. `ClientId` is public (exposed in front-end code), not a secret.
- Endpoint `POST /api/auth/google` accepts `{ idToken }` → validates → issues a token pair (same `LoginResponse` as regular login).
- If Google ever deprecates GIS — we will have to rewrite to Authorization Code flow. Low risk: GIS is their strategic direction.

---

## ADR-BACK-AUTH-011: `GoogleId` as denormalized field on User instead of `AspNetUserLogins`

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

## ADR-BACK-AUTH-012: Rate limiting — in-memory FixedWindow per IP, single strict policy

**Decision:** Sensitive auth endpoints (register, login, google login, forgot-password, reset-password, resend-confirmation, confirm-email) are limited by the built-in `Microsoft.AspNetCore.RateLimiting` — **5 requests per 15 minutes per IP**, FixedWindowLimiter, `QueueLimit = 0`. Refresh and logout are not limited. Exceeding limit → 429 `ProblemDetails` + `Retry-After` header.

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
- In-memory counters — **counters will drift upon scale-out**. An attacker might get 5×N attempts on N instances. Documented trade-off, not critical while single-instance.
- `HttpContext.Connection.RemoteIpAddress` behind a reverse proxy returns the proxy's IP, not the client's. When deploying to Azure App Service / Container Apps, you **must** add `UseForwardedHeaders()` — otherwise, all users share one partition key and rate limiting becomes a global counter. Task D-06.5 in TODO.

---

## ADR-BACK-AUTH-013: Authorization checks live in handlers, not controllers

**Decision:** Checks for "can the current user perform this operation on this resource" (owner check, role check) are performed inside command/query handlers via `ICurrentUserService`. The Controller does not take this responsibility — it only handles HTTP concerns (read body, return ToActionResult).

**Why:**
- The controller does not know `course.InstructorId` without fetching it via a repository. If the controller fetches it — it effectively performs part of the handler's job → SRP violation.
- ASP.NET `[Authorize(Policy = ...)]` works well for static rules (role in claims, claim value). Owner checks require fetching the resource → resource-based authorization → dynamic → natural place is the handler.
- Handler returns `Result.Fail(new ForbiddenError(...))` → `ToActionResult()` maps to 403. Aligned with existing pipeline (DECISIONS_ARCHITECTURE.md ADR-002, ADR-004).
- One place to look — all business rules are visible in a single layer.

**Alternatives:**
- Authorization in controller via custom `IAuthorizationRequirement + AuthorizationHandler` — official ASP.NET approach for resource-based auth. Rejected: adds a layer of indirection with no benefit for a solo project with a single owner-check type (`InstructorId`).
- Authorization in domain entity method (`course.UpdateDetails(..., requestingUserId)`) — mixes identity knowledge with entity business logic, violates SRP.

**Consequences:**
- Every mutating handler performs 2 checks: `currentUser.UserId is null` (401) + owner/admin (403).
- One extra fetch on mutation for an entity that would be fetched anyway — acceptable trade-off.
- As handlers grow, this can be extracted into an extension method: `ResultExtensions.EnsureOwnership(Guid resourceOwnerId, ICurrentUserService user)` — cosmetic refactor, non-blocker.

---

## ADR-BACK-AUTH-014: Email confirmation soft restriction via ASP.NET Core authorization policy

**Decision:** After registration, the user is automatically logged in, but the email remains unconfirmed. A persistent banner in the UI reminds them to confirm their email. Write-actions with real platform impact are protected by a named policy `EmailConfirmed`, which checks the `email_verified` claim in the JWT. Unconfirmed users can freely browse the catalog and their profile; specific endpoints return 403 when the policy is not met.

**Gated endpoints:**

| Endpoint | Reason |
|---|---|
| `POST /api/enrollments` | Enrolling in a free course — all downstream actions (progress, tests, certificates) cascade from this gate. |
| Stripe checkout (Phase 9) | Paid course enrollment — successful payment creates the same `Enrollment` record. |
| `POST /api/instructor-applications` | Triggers admin review; spam applications from unverified emails are a moderation risk. |
| `POST /api/courses/{id}/reviews` | Public content tied to a real identity. |
| `PUT /api/courses/{id}/reviews/{reviewId}` | Same — editing public content. |
| `POST /api/messages/conversations/start-or-get` | First point of human contact; requires trust. |
| `POST /api/messages/conversations/{id}/messages` | Same. |

**Free (not blocked):** catalog browsing, course page, reading reviews, profile (read + edit), AI chat. Progress / tests / certificates cascade from Enrollment — a separate gate isn't needed.

**Why:**
- **Controller-level concern, not domain concern.** "Is the user's identity confirmed?" is an authentication/authorization question, not business logic. The natural place is an `[Authorize]` attribute (same level as role checks), not inside handlers — aligns with ADR-BACK-AUTH-013, which reserves handler-level auth checks for resource-based (owner) decisions.
- **One mechanism, not seven.** A single named policy decorates 7 endpoints. The alternative (checking `ICurrentUserService.IsEmailConfirmed` in every handler) scatters auth logic across the Application layer and complicates auditing.
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
- New named policy `EmailConfirmed` registered in `AddApiServices` (`Learnix.API`).
- 7 endpoints receive `[Authorize(Policy = "EmailConfirmed")]` on top of existing `[Authorize]`.
- Frontend: `isEmailConfirmed: boolean` added to auth store; persistent banner displayed if `false`; on 403 from gated endpoint — a modal "Confirm email first" with a resend button.

---

## ADR-BACK-AUTH-015: Infrastructure gets FrameworkReference to Microsoft.AspNetCore.App

**Decision:** `Learnix.Infrastructure.csproj` declares `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. This grants access to ASP.NET Core shared framework assemblies (`Microsoft.AspNetCore.Identity`, `Microsoft.AspNetCore.Authentication.*`, etc.) needed to implement auth-related services.

**Why:**
- The `AddIdentity<,>()` extension method lives in the `Microsoft.AspNetCore.Identity` assembly, which is part of the shared framework, not a separate NuGet package.
- A Class library targeting `Microsoft.NET.Sdk` (not `.Web`) does not have access to the shared framework by default, even if `Microsoft.AspNetCore.Identity.EntityFrameworkCore` packages are installed.
- `FrameworkReference` — standard mechanism to access the shared framework from non-Web projects (documented Microsoft approach).

**Alternatives:**
- Move Identity-related code to a separate project `Learnix.Infrastructure.Identity` with `Sdk="Microsoft.NET.Sdk.Web"` — artificial separation, EF configurations for User logically belong to the core Infrastructure, adds DI complexity without value.
- Move Identity setup to the API project (where `Sdk` is already Web) — violates "Infrastructure implements all technical concerns", scatters auth logic across layers.

**Consequences:**
- `Learnix.Infrastructure` transitively has access to all of `Microsoft.AspNetCore.App` (MVC, SignalR, Authentication middleware). This is formally a scope expansion, but practically we only use Identity and (in the future) JWT bearer authentication.
- Zero runtime overhead — the shared framework is already present on the host via the API project.
- Same compromise as ADR-BACK-AUTH-002 (User : IdentityUser): formally less clean, pragmatically necessary.

---

## Request Lifecycle (Authorization Process)

Simulation of a request journey from the client to business logic execution:

1. **Client (Frontend):** 
   Sends a request to a protected endpoint, adding `Authorization: Bearer <access_token>` in the headers. If it's a refresh token request, it automatically sends the HttpOnly cookie `learnix_refresh` via the browser (credentials: 'include').

2. **Middleware (ASP.NET Core JwtBearer):** 
   The request hits `JwtBearerMiddleware`. The token is validated: checks signature (using `Jwt.Secret`), expiration (`exp`), and integrity. `ClaimsPrincipal` is constructed from JWT claims and assigned to `HttpContext.User`. If the token is invalid or expired — middleware returns `401 Unauthorized` and the request goes no further.

3. **Controller ([Authorize] and Policies):** 
   The request reaches the controller. The `[Authorize]` attribute verifies authentication (whether a valid user is present). If the endpoint also has `[Authorize(Policy = "EmailConfirmed")]`, it verifies the policy (presence of `email_verified` = `true` claim). If policy verification fails — the controller returns `403 Forbidden`. The controller reads the request payload and dispatches a command/query via MediatR (`sender.Send(...)`).

4. **Application Handler (Business Logic):** 
   The command/query handler injects `ICurrentUserService` (which reads `HttpContext.User` under the hood). 
   - The handler checks the current user: `if (currentUser.UserId is null) return Result.Fail(new AuthenticationError());`
   - Performs owner check: E.g., whether the course belongs to the current `InstructorId`. `if (course.InstructorId != currentUser.UserId) return Result.Fail(new ForbiddenError());`
   
5. **Service Layer (Infrastructure/Identity):** 
   If it's a login or registration request, the handler calls `IUserAuthenticationService` or `IUserRegistrationService` to validate passwords or generate new tokens (which in turn utilize `UserManager` from ASP.NET Core Identity).

---

## ADR-BACK-AUTH-016: 6-Digit OTP for Email Confirmation instead of Magic Link

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

**Decision:** The hashing mechanism for Refresh Tokens was upgraded from a standard `SHA256` to `HMAC-SHA256` utilizing a globally configured Secret Key (Pepper) defined in `Jwt:RefreshTokenSecret`.

**Why:**
- While a 64-byte random string (512-bit entropy) is already mathematically immune to brute-force attacks even with standard SHA256, adding a Pepper provides absolute immunity against **offline verification**.
- In the event of a database leak, an attacker would possess the token hashes. Without the Pepper (which resides only in the application's configuration/environment variables and not in the database), the attacker cannot verify whether a stolen raw refresh token matches any hash in the database.
- Key Separation Principle: `Jwt:Secret` is used exclusively for signing Access Tokens (JWTs), while `Jwt:RefreshTokenSecret` is used exclusively for hashing Refresh Tokens. 

**Alternatives:**
- **Standard SHA256 (Previous Implementation):** Secure against brute force due to high entropy, but allows an attacker with a leaked database to verify intercepted raw tokens offline.
- **Salting (bcrypt/Argon2):** Unnecessary for machine-generated high-entropy tokens. Salts protect low-entropy secrets (like human passwords) against rainbow tables.

**Consequences:**
- `JwtSettings` requires a new configuration property `RefreshTokenSecret`.
- CI/CD pipelines and deployment documentation must include the provisioning of `PROD_JWT_REFRESH_SECRET`.
- The `HashRefreshToken` method in `JwtTokenService` now requires the instantiation of `HMACSHA256` with the provided Pepper.
