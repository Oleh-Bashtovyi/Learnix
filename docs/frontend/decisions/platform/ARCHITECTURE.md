# Learnix — Frontend Architecture Decision Records (Architecture)

> Format: Decision → Why → Alternatives.
> Backend architectural decisions are in `docs/backend/decisions/platform/ARCHITECTURE.md`.

---

## ADR-FRONT-ARCH-001: Layer-based Structure with Feature Co-location

**Context:** A new codebase needs one of two shapes: organize by technical layer (`api/`, `components/`,
`pages/`, …) or by feature (`features/courses/`, `features/enrollments/`, …).

**Decision:**
The `src/` directory is organized by layers (api, components, pages, hooks, store, schemas, types, utils). Feature-specific files live inside each layer (e.g., `api/courses.api.ts`, `schemas/course.schema.ts`).

**Why:**
- Layer-based is simpler to start with and matches typical React tutorial setups.
- A pure feature-sliced architecture (e.g., `features/courses/api`, `features/courses/components`) scales better for huge apps, but for an LMS with ~20-30 features, layer-based remains manageable without excessive nesting.

**Alternatives:**
- Pure feature-based structure — discarded as slightly overkill for v1.
- Layer-based without page-level co-location — discarded because the `components/` folder would grow to 100+ files and become unmaintainable.

---

## ADR-FRONT-ARCH-002: Page Co-location and Ad-Hoc Components

**Context:** Layer-based structure (ADR-FRONT-ARCH-001) still needs a rule for where a component used
by only one page belongs, or `components/` stops being "shared" in any meaningful sense.

**Decision:**
- Pages are grouped by role: `pages/public/`, `pages/student/`, `pages/instructor/`, `pages/admin/`.
- Ad-hoc components that are only used by one specific page live next to that page.
  - **1-3 helper components:** Kept as flat files next to the page component.
  - **4+ helper components:** Grouped in a `components/` subfolder inside the page directory.
- Once an ad-hoc component is needed by a second page, it is promoted to `components/common/`.

**Why:**
- Keeps `components/common/` clean and truly shared.
- Co-location makes deleting or refactoring a feature much easier since all its UI parts are in one place.

---

## ADR-FRONT-ARCH-003: Routing — React Router v7 with Nested Layouts and Guards

**Context:** Role-gated pages need a single place to decide "is this visitor allowed here", instead of
every page checking auth state itself.

**Decision:**
- We use **React Router v7** with `createBrowserRouter`.
- Route protection is handled by guard components — `RequireRole` (`components/common/auth/`) checks the
  Zustand auth store and redirects unauthorized visitors, via the shared `APP_ROUTES` dictionary
  (ADR-FRONT-ARCH-005), never a hardcoded path.
- Lazy loading is applied to all pages to keep the initial bundle small (e.g., students don't download the admin dashboard code).

**Why:**
- Centralized route guards simplify the components themselves.
- React Router v7 provides the modern `Loader` API if we ever need to transition data-fetching to the router level, but currently we rely on React Query inside components.

**Alternatives:**
- Context-based routing or older `react-router` versions. V7 is the modern standard.

---

## ADR-FRONT-ARCH-004: Tooling & Core Libraries

**Context:** A baseline tooling stack needs to be picked once so contributors aren't debating package
managers or bundlers on every PR.

**Decision:**
We standardize on the following core tooling stack:
- **Package Manager:** `npm` (ships with Node LTS).
- **Bundler:** Vite 8.
- **Code Quality:** ESLint + Prettier + Husky + lint-staged (with `prettier-plugin-tailwindcss` for auto-sorting).
- **Icons:** `lucide-react` (bundled with shadcn).
- **File Uploads:** Direct to Azure Blob Storage via presigned SAS URLs.

**Why:**
- Ensures consistent developer experience and eliminates "which library to use" debates.
- `npm` avoids cross-platform workspace issues that sometimes plague `pnpm` on Windows.
- Direct uploads to Azure bypass the backend, saving server memory and bandwidth for large video uploads.

---

## ADR-FRONT-ARCH-005: Centralized Route Dictionary (APP_ROUTES)

**Context:** Hardcoded path strings scattered across `<Link>`, `useNavigate` and `Route` definitions
break silently the moment a URL changes, with no compiler error to catch the miss.

**Decision:**
All routing paths in `<Link>` components, `useNavigate`, and `Route` definitions must use the centralized `APP_ROUTES` dictionary from `src/routes/paths.ts` instead of hardcoded strings. Dynamic routes use factory functions (e.g., `APP_ROUTES.public.courseDetail(courseId)`).

**Shared Messaging Page, Role-Specific Routes:**
Student, instructor and admin each get their own path (`APP_ROUTES.student.messages` = `/messages`,
`.instructor.messages` = `/instructor/messages`, `.admin.messages` = `/admin/messages`), but all
three mount the same `MessagesPage` component — the UI adapts based on the authenticated user's role,
not the route.

**Why:**
- Prevents broken links when a URL structure changes, as we only need to update the dictionary.
- Provides TypeScript autocomplete for all available application routes.
- Factory functions ensure the correct parameters are passed for dynamic paths.

**Alternatives:**
- *Template-only dictionary with `generatePath`*: We considered keeping only the path templates (e.g., `/courses/:id`) in the dictionary and using `generatePath('/courses/:id', { id })` from React Router everywhere. Discarded because `generatePath` lacks strict type safety for the parameter names (depending on the router version setup), and factory functions are more explicit and ergonomic.
- *Third-party routing libraries (`typesafe-routes` or `path-to-regexp`)*: Overkill and adds unnecessary bundle size for our needs.
