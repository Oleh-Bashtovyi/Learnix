# Learnix — Frontend Architecture Decision Records (Forms)

> Format: Decision → Why → Alternatives.

---

## ADR-FRONT-FORMS-001: Zod Schemas as Source of Truth for Forms

**Decision:**
- Zod schemas define the structure and validation rules for all forms.
- The TypeScript `FormValues` type is inferred directly from the Zod schema.
- DTOs (Data Transfer Objects) mapping to the backend are defined as separate standard TypeScript interfaces in `src/types/`.
- The transformation from `FormValues` to `DTO` occurs explicitly in the form's `onSubmit` handler.

**Why:**
- The shape of a form often differs from the API contract (e.g., a comma-separated string of tags in the UI vs. an array of strings in the DTO).
- Trying to force a single type for both UI and API leads to awkward compromises. Explicit mapping in `onSubmit` is highly visible and easy to debug.

**Consequences:**
- Zod is strictly for form validation. We do *not* use Zod to validate incoming API responses, as that adds unnecessary runtime overhead.
- `src/types/` contains manually written interfaces for DTOs, grouped by domain (e.g., `course.types.ts`, `user.types.ts`).

---

## ADR-FRONT-FORMS-002: Three-Tier Error Handling Strategy

**Decision:**
Error handling follows three distinct levels:
1. **Field-Level (Form Validation):** The `isValidationError` guard and `setApiFieldErrors` utility parse the backend's `ProblemDetails` (RFC 7807) `errors` dictionary and map them directly to `react-hook-form` field errors.
2. **Business Level (Toasts):** Global `onError` handlers in React Query catch standard business errors and display a user-friendly toast via `sonner` using `getErrorMessage`.
3. **Application Level (Error Boundaries):** Unhandled JS crashes are caught by a root-level `<ErrorBoundary>` to prevent blank screens.

**Status Code Mapping to UI:**

| HTTP Status | What it is | Where it is shown |
|---|---|---|
| 400 with `errors` | Backend validation failure | Inline under form fields |
| 400 without `errors` | Business logic error | Toast error |
| 401 | Token expired | Interceptor silently refreshes — user doesn't see it |
| 403 | Forbidden | Toast + redirect |
| 404 | Not Found | Toast or NotFoundPage (depends on context) |
| 409 | Conflict | Toast error (e.g., "Already enrolled") |
| 500+ | Server Error | Toast "Something went wrong" + optional Error Boundary |

**Why:**
- Different errors require different UX. A server validation error needs to show exactly which input failed. A network timeout just needs a toast notification.
- Global handlers reduce boilerplate in individual components.
- `ProblemDetails` (RFC 7807) is the backend standard and maps cleanly to the UI.
