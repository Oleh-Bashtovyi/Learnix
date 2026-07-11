# Learnix — Backlog

> Active work. `TODO.md` is the closed build log of phases 1–8; everything that remains lives here.
>
> The remaining work no longer splits by feature — it cuts across the whole product — so it is
> grouped by **kind of work** instead: tests, bugs, design, infrastructure, docs.
>
> Status: `todo` · `in progress` · `done` · `wontfix`
> Priority: `high` · `medium` · `low`

---

## Snapshot (2026-07-11)

| Area | State |
|---|---|
| Features | Complete. Backend + frontend phases 1–8 all `done` or `CANCELED`. |
| Tests | 344 tests, 2 unit projects. Domain layer fully covered; ~20 of 121 handlers covered. No integration tests, no frontend tests. |
| Deployment | Live on Azure. Key Vault, custom domain and App Insights not done. |
| Known defects | 2 open (below), both in the AI-chat SSE path. |

---

## 1. Tests

The single biggest gap. 121 handlers exist in `Learnix.Application`; 25 test files cover roughly
20 of them. Coverage is concentrated in Auth, AiChat and the test-attempt flow — the features
built last. The features built first have none.

### 1.1 Application — features with zero handler tests

| # | Feature | Handlers | Priority | Status | Notes |
|---|---|---|---|---|---|
| T-01 | Courses (create, update, publish, delete, instructor queries) | 17 | high | todo | Only `GetPublicCourses` query + validator are tested. The write side — the core of the product — is untested. |
| T-02 | Categories | 13 | medium | todo | Mostly CRUD; cheap to cover, good ratio. |
| T-03 | Lessons (update, delete, reorder, queries) | 10 | high | todo | The 3 `Create*Lesson` commands are tested; nothing else is. |
| T-04 | Messaging (student ↔ instructor) | 6 | medium | todo | Authorization matters here — who may read a conversation. |
| T-05 | Notifications | 5 | low | todo | |
| T-06 | Sections | 4 | medium | todo | Reorder logic is worth a test. |
| T-07 | Wishlist | 4 | low | todo | |
| T-08 | Certificates | 4 | medium | todo | Issue-once rule is worth a test. |
| T-09 | Enrollments | 3 (1 tested) | high | todo | Free vs paid gating. `GetContinueLearning` is covered; enroll is not. |
| T-10 | LessonProgress | 2 | medium | todo | Course-completion trigger → certificate + achievement chain. |
| T-11 | Achievements | 2 | low | todo | Evaluation runs through the Outbox; test the handler, not the worker. |
| T-12 | Users / Admin / Uploads / Config | 12 (2 tested) | medium | todo | `AdminRemoveRole` is covered. Ban/unban, role grant, SAS issuing are not. |

### 1.2 Domain — done (2026-07-11)

Every entity carrying behaviour now has unit tests: `Category`, `Course`, `Lesson`, `Question`,
`User`, `TestAttempt`, `Enrollment`, `LessonProgress`, `CourseReview`, `Certificate`, `Payment`,
`InstructorApplication`, `Notification`, `RefreshToken`. 160 tests in `Learnix.Domain.UnitTests`.

`WishlistItem` is intentionally untested — it is a pure join record with no methods and nothing to
assert beyond the constructor.

### 1.3 Test infrastructure

| # | Item | Priority | Status | Notes |
|---|---|---|---|---|
| T-17 | Integration / endpoint tests (`WebApplicationFactory` + Testcontainers) | medium | todo | No such project exists. Would cover auth policies, rate limiting, ProblemDetails mapping and the SAS→commit upload flow — none of which unit tests can reach. Decide scope before starting: a handful of critical paths, not everything. |
| T-18 | Frontend tests | low | wontfix | Deliberate: no frontend test setup, and adding one now is not the best use of remaining time. Stated as a scope decision, not an oversight. |

---

## 2. Bugs

| # | Item | Priority | Status | Notes |
|---|---|---|---|---|
| B-01 | `GeminiChatProvider` throws on a provider error instead of yielding `ProviderErrorEvent` | high | todo | The exception escapes **after** the SSE headers are written, so `ExceptionHandlingMiddleware` cannot set a status and logs `StatusCode cannot be set because the response has already started`. The client sees a truncated stream. Client side is already defended (a stream ending with no terminal event is treated as an error); the server side is the real fix. Reproduces reliably by exhausting the Gemini free-tier daily quota. |
| B-02 | `tool_use_end` reports `resultsCount: 0` for `get_my_test_review` | low | todo | The tool returns a single object, not a list, so the count field reads 0 even on success. Cosmetic — the UI labels the step from `toolName` — but the event lies. Either count 1 for object results or omit the field for non-list tools. |

---

## 3. Design & UX

| # | Item | Priority | Status | Notes |
|---|---|---|---|---|
| D-01 | Design pass — fill in as issues are found | medium | todo | Placeholder. Add concrete rows (page, what's wrong, what it should be) rather than letting this stay a vague bucket. |

---

## 4. Infrastructure

Carried over from `TODO.md` — Deploy phase 3.

| # | Item | Priority | Status | Notes |
|---|---|---|---|---|
| I-01 | Azure Key Vault for secrets | medium | todo | Secrets currently live in Container Apps env vars. |
| I-02 | Custom domain + SSL | low | todo | |
| I-03 | Application Insights (Serilog sink) | low | todo | |
| I-04 | Terraform: Mongo module | in progress | — | `infrastructure/mongo.tf` is new and uncommitted; `providers.tf` and `variables.tf` are modified. |

---

## 5. Documentation

| # | Item | Priority | Status | Notes |
|---|---|---|---|---|
| X-01 | `FEATURES.md` is thin | medium | in progress | 209 lines for the whole LMS; bullet-level, no acceptance criteria or edge cases. Deepen it lazily — rewrite a section whenever work takes you into that feature. Course catalog is the first section done, and sets the format. |
| X-02 | `TECH_DEBT.md` is an empty stub | low | todo | Header only, zero entries. Either fill it or fold it into this file — two half-empty backlogs are worse than one. |
| X-03 | `TODO.md` summary counts are stale | low | todo | Claims 4 backend tasks remaining; all 54 are `done` or `CANCELED`. Fix the table and mark the file closed. |

---

## 6. Uncommitted work

Branch `dev` carries two finished, verified, **uncommitted** changes:

1. **AI-tutor test review** — `get_my_test_review` tool, `GetTestReviewForAi` query/handler/DTO,
   `reviewAvailable` + `submittedAttempts` on `TestInfoDto`, prompt rules, frontend tool label and
   i18n, ADR-CHAT-012. Plus a fix in `useAiChat.ts`: a stream ending without `message_end`/`error`
   no longer leaves `isStreaming` stuck true.
2. **Tool names extracted to constants** — `ChatToolNames.cs`, `aiChat.constants.ts`,
   `ChatToolJson.cs`; every tool's `Definition` now derives its name from its own `Name` property.

Both were verified live against the running app on 2026-07-11, including the test-review happy path.
