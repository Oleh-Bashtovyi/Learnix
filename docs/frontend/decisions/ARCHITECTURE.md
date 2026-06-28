# Learnix — Frontend Architecture Decision Records (Architecture)

> Format: Decision → Why → Alternatives.
> Backend architectural decisions are in `docs/backend/DECISIONS_ARCHITECTURE.md`.

---

## ADR-FRONT-ARCH-001: Layer-based Structure with Feature Co-location

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

**Decision:**
- We use **React Router v7** with `createBrowserRouter`.
- Route protection is handled by a `<RequireRole />` guard component that checks the Zustand auth store and redirects if unauthorized.
- Lazy loading is applied to all pages to keep the initial bundle small (e.g., students don't download the admin dashboard code).

**Code Fragment (RequireRole Guard):**
```tsx
// src/components/common/RequireRole.tsx
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore, type UserSummary } from '@/store/auth.store';

interface Props {
    roles: UserSummary['roles'];
    children: React.ReactNode;
}

export function RequireRole({ roles, children }: Props) {
    const { user, isInitializing } = useAuthStore();
    const location = useLocation();

    if (isInitializing) return null;

    if (!user) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    const hasRole = user.roles.some((r) => roles.includes(r));
    if (!hasRole) {
        return <Navigate to="/" replace />;
    }

    return <>{children}</>;
}
```

**Why:**
- Centralized route guards simplify the components themselves.
- React Router v7 provides the modern `Loader` API if we ever need to transition data-fetching to the router level, but currently we rely on React Query inside components.

**Alternatives:**
- Context-based routing or older `react-router` versions. V7 is the modern standard.

---

## ADR-FRONT-ARCH-004: Tooling & Core Libraries

**Decision:**
We standardize on the following core tooling stack:
- **Package Manager:** `npm` (ships with Node LTS).
- **Bundler:** Vite.
- **Code Quality:** ESLint + Prettier + Husky + lint-staged (with `prettier-plugin-tailwindcss` for auto-sorting).
- **Icons:** `lucide-react` (bundled with shadcn).
- **Dates:** `date-fns` (tree-shakable).
- **File Uploads:** Direct to Azure Blob Storage via presigned SAS URLs.

**Why:**
- Ensures consistent developer experience and eliminates "which library to use" debates.
- `npm` avoids cross-platform workspace issues that sometimes plague `pnpm` on Windows.
- Direct uploads to Azure bypass the backend, saving server memory and bandwidth for large video uploads.
