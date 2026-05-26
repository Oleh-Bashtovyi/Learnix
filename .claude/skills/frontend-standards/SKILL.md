---
name: frontend-standards
description: Frontend coding standards, pre-task checklist, folder structure, component conventions, state management rules, API layer, forms, styling, localization, error handling, and anti-patterns for the Learnix React 19 + TypeScript frontend. Use when implementing frontend features, creating components, writing hooks, adding pages, working with TanStack Query, Zustand, React Hook Form, Tailwind, or localization.
when_to_use: frontend, React, TypeScript, component, page, hook, store, Zustand, TanStack Query, React Query, form, Zod, Tailwind, shadcn, localization, axios, routing, SSE, upload
---

# Learnix Frontend Standards

## Pre-Task Checklist

Before implementing **any** frontend feature, complete these steps in order:

1. **Read `TODO.md`** — identify the exact task(s), their phase, and current status.
2. **Read `docs/FEATURES.md`** — understand the functional spec for the feature being built.
3. **Read `ARCHITECTURE_FRONTEND.md`** — review folder structure, component co-location rules, state split, and API layer conventions.
4. **Read `DECISIONS_FRONTEND.md`** — review all FADRs; note any that apply to the current task (styling, routing, forms, error handling, localization, etc.).
5. **Check for mockups** — look in `mockups/` for the feature. If none exists, match the visual style of existing pages.
6. **Check available backend endpoints** — read the relevant `Learnix.Backend/Learnix.API/Controllers/*.cs` files. Verify route prefix, HTTP methods, request/response shapes, and auth requirements before writing any API calls.

---

## Post-Task Checklist

After completing **any** frontend task:

1. Run `npx tsc --noEmit` in `learnix-client/` — fix all TypeScript errors before finishing.
2. Run `npm run format` in `learnix-client/` — auto-format + Tailwind class sort.
3. **Update `TODO.md`** — mark the task(s) as `[x]`. Add a note if the implementation deviated from the spec.
4. **Update `DECISIONS_FRONTEND.md`** — if a new architectural decision was made, add a `FADR-XXX` entry.

---

## Folder Structure — Quick Reference

```
learnix-client/src/
├── api/              ← Axios instance + typed API modules (*.api.ts)
├── components/
│   ├── ui/           ← shadcn/ui only — generated via CLI, never hand-written
│   ├── common/       ← reusable on 2+ pages
│   └── layout/       ← layouts, Header, Sidebar, Footer
├── pages/
│   ├── public/       ← no auth required
│   ├── student/      ← Student role
│   ├── instructor/   ← Instructor role
│   └── admin/        ← Admin role
├── hooks/            ← custom React hooks (use*.ts)
├── store/            ← Zustand stores (*.store.ts)
├── schemas/          ← Zod schemas (*.schema.ts) — source of truth for FormValues
├── types/            ← TypeScript DTO interfaces (*.types.ts)
├── utils/            ← pure utilities (cn.ts, errors.ts, formatDate.ts, etc.)
├── routes/           ← React Router config
└── const/
    └── localization/ ← ALL UI text — one file per page (*.ts)
```

### Component co-location rules (FADR-002)

- **1–3 ad-hoc components** for a page → flat files alongside the page file
- **4+ ad-hoc components** → put in a `components/` subfolder inside the page folder
- Component used on a **second page** → move it to `components/common/`
- Never create files manually in `components/ui/` — use `npx shadcn-ui add <component>`

---

## File Naming Conventions

| Item | Convention | Example |
|---|---|---|
| Component | PascalCase `.tsx` | `CourseCard.tsx` |
| Hook | camelCase, `use` prefix | `useAuth.ts` |
| Store | camelCase, `.store.ts` | `auth.store.ts` |
| API module | camelCase, `.api.ts` | `courses.api.ts` |
| Schema | camelCase, `.schema.ts` | `course.schema.ts` |
| Types | camelCase, `.types.ts` | `course.types.ts` |
| Localization | camelCase | `landingPage.ts` |
| Utility | camelCase | `formatDate.ts` |

---

## Styling (FADR-009)

**100% Tailwind. No SCSS, no CSS Modules.**

- Always use **semantic tokens**: `bg-primary`, `text-foreground`, `border-border`, `bg-muted`, `text-muted-foreground`
- **Never** hardcode colors: ~~`bg-blue-600`~~, ~~`text-gray-500`~~
- Use `cn()` from `@/utils/cn` for conditional classes — never string concatenation
- Mobile-first: base classes for mobile, `md:` / `lg:` for larger screens
- Dark mode is handled automatically via CSS variables — never check theme in component logic

```tsx
import { cn } from '@/utils/cn';

interface CourseCardProps {
  course: CourseDto;
  className?: string;
}

export function CourseCard({ course, className }: CourseCardProps) {
  return (
    <div
      className={cn(
        'rounded-xl border border-border bg-card p-6 transition-shadow',
        'hover:shadow-md',
        className,
      )}
    >
      <h3 className="font-heading text-lg font-semibold text-foreground">
        {course.title}
      </h3>
      <p className="mt-2 text-sm text-muted-foreground">{course.description}</p>
    </div>
  );
}
```

---

## Localization (FADR-012)

**All UI text lives in `src/const/localization/<page>.ts`. Zero hardcoded strings in components.**

```ts
// src/const/localization/courseCatalog.ts
export const COURSE_CATALOG = {
  FILTERS: {
    heading: 'Filters',
    clear: 'Clear all',
  },
  EMPTY: {
    title: 'No courses found',
    subtitle: 'Try adjusting your filters',
  },
} as const;
```

```tsx
// In component:
import { COURSE_CATALOG } from '@/const/localization/courseCatalog';

<h2>{COURSE_CATALOG.FILTERS.heading}</h2>
```

- One file per page / feature area
- `as const` — TypeScript catches typos at compile time
- SCREAMING_SNAKE_CASE for top-level export, camelCase for nested keys

---

## State Management (FADR-005)

| What | Where | Example |
|---|---|---|
| Server data (from API) | TanStack Query | courses list, user profile, enrollments |
| Auth token + user | `auth.store` (Zustand) | `accessToken`, `user`, `isInitializing` |
| UI state shared across components | `ui.store` (Zustand) | `isSidebarOpen`, `isChatOpen` |
| Theme | `theme.store` (Zustand, persisted) | `'light' \| 'dark'` |
| Local component state | `useState` | dropdown open, accordion expanded |
| Form values + validation | `react-hook-form` | field values, errors, touched |

**Rules:**
- Never put API data in Zustand — that's TanStack Query's job
- Use `ui.store` only when state is shared between **unrelated** components. Start with `useState`, migrate to `ui.store` if a second component needs it
- Zustand stores are accessed in axios interceptors via `useAuthStore.getState()` (not hooks)

---

## API Layer (FADR-004)

**Components never import axios directly.** Always use typed API modules.

```ts
// src/api/courses.api.ts
import { api } from './axios.instance';
import type { CourseDto, CreateCourseRequest } from '@/types/course.types';
import type { PaginatedResult } from '@/types/api.types';

export const coursesApi = {
  getAll: (filters: CourseFilters) =>
    api.get<PaginatedResult<CourseDto>>('/courses', { params: filters }).then((r) => r.data),

  getById: (id: string) =>
    api.get<CourseDto>(`/courses/${id}`).then((r) => r.data),

  create: (data: CreateCourseRequest) =>
    api.post<{ courseId: string }>('/courses', data).then((r) => r.data),
};
```

**React Query hook wrapping the API:**
```ts
// src/hooks/useCourses.ts
import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';

export function useCourses(filters: CourseFilters) {
  return useQuery({
    queryKey: queryKeys.courses.list(filters),
    queryFn: () => coursesApi.getAll(filters),
  });
}
```

**Query invalidation on mutation:**
```ts
// src/hooks/useCreateCourse.ts
export function useCreateCourse() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: coursesApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.lists() });
      toast.success(SOME_PAGE.COURSE_CREATED);
    },
  });
}
```

**Query keys** are hierarchical — defined in `src/api/queryKeys.ts`. Always use them, never inline string arrays.

---

## Forms (FADR-006)

Zod schema is the **source of truth for form types**. DTO is a separate interface matching the backend contract.

```ts
// src/schemas/course.schema.ts
export const createCourseSchema = z.object({
  title: z.string().min(3).max(200),
  categoryId: z.string().uuid(),
  price: z.number().min(0),
});
export type CreateCourseFormValues = z.infer<typeof createCourseSchema>;

// src/types/course.types.ts
export interface CreateCourseRequest {
  title: string;
  categoryId: string;
  price: number;
}
```

```tsx
const form = useForm<CreateCourseFormValues>({
  resolver: zodResolver(createCourseSchema),
});

const onSubmit = (values: CreateCourseFormValues) => {
  const request: CreateCourseRequest = { ...values }; // explicit transform if shapes differ
  createCourse.mutate(request);
};
```

- Zod schemas are **not** used to type backend responses — those are plain TypeScript interfaces in `types/`

---

## Error Handling (FADR-007)

Three levels — match the level to the error type:

| HTTP | Error type | Where shown |
|---|---|---|
| 400 with `errors` dict | Validation | Inline under form fields via `form.setError()` |
| 400 without `errors` | Business error | Toast error |
| 401 | Token expired | Axios interceptor handles silently (refresh → retry) |
| 403 | Forbidden | Toast + redirect |
| 404 | Not found | Toast or NotFoundPage depending on context |
| 409 | Conflict | Toast error (e.g. "Already enrolled") |
| 500+ | Server error | Toast "Something went wrong" |

```tsx
const onSubmit = async (values: CreateCourseFormValues) => {
  try {
    await createCourse.mutateAsync(request);
    navigate('/instructor/courses');
  } catch (error) {
    if (isValidationError(error)) {
      Object.entries(error.response!.data.errors!).forEach(([field, messages]) => {
        form.setError(field.charAt(0).toLowerCase() + field.slice(1) as any, {
          message: messages.join('. '),
        });
      });
    }
    // Non-validation errors handled by global QueryClient onError → toast
  }
};
```

Utilities in `src/utils/errors.ts`: `isValidationError()`, `getErrorMessage()`.

Global mutation error handler (already set up in `main.tsx`):
```ts
mutations: {
  onError: (error) => {
    if (!isValidationError(error)) toast.error(getErrorMessage(error));
  },
},
```

---

## Routing (FADR-003)

- Route guards as **layout components** via `<Outlet />` — not per-page wrappers
- Lazy loading for every page via `React.lazy()` + `Suspense`
- `ProtectedRoute` handles: `isInitializing` spinner → auth check → role check

```tsx
const router = createBrowserRouter([
  { element: <PublicLayout />, children: publicRoutes },
  {
    element: <ProtectedRoute requiredRole="Student" />,
    children: [{ element: <StudentLayout />, children: studentRoutes }],
  },
]);
```

---

## SSE Streaming

For streaming responses (AI chat), use `fetch` with `ReadableStream`. **Never use `EventSource`** — it doesn't support custom headers (Authorization).

```ts
const response = await fetch(`${env.API_URL}/ai/chat`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${useAuthStore.getState().accessToken}`,
  },
  body: JSON.stringify({ message }),
});

const reader = response.body!.getReader();
const decoder = new TextDecoder();

while (true) {
  const { done, value } = await reader.read();
  if (done) break;
  const chunk = decoder.decode(value);
  // handle chunk
}
```

---

## File Uploads

All uploads use the **3-step SAS URL flow**. Never send files directly to the backend via multipart form.

```ts
// Step 1: Request upload URL
const { uploadUrl, blobPath } = await uploadsApi.requestUrl({ target: 'CourseCover', contentType: 'image/jpeg' });

// Step 2: Upload directly to Azure Blob
await fetch(uploadUrl, { method: 'PUT', body: file, headers: { 'x-ms-blob-type': 'BlockBlob' } });

// Step 3: Send blobPath to backend in the relevant command
await coursesApi.update(courseId, { ...data, coverBlobPath: blobPath });
```

Upload targets: `Avatar` (5MB, jpeg/png/webp), `CourseCover` (10MB, jpeg/png/webp), `LessonVideo` (2GB, mp4/webm), `Certificate` (5MB, pdf).

---

## Component Props Convention

Always use a **named interface** for props — never inline types.

```tsx
// Good
interface CourseCardProps {
  course: CourseDto;
  onEnroll?: (courseId: string) => void;
  isCompact?: boolean;
}

export function CourseCard({ course, onEnroll, isCompact = false }: CourseCardProps) { ... }

// Bad — inline props
export function CourseCard({ course }: { course: CourseDto }) { ... }
```

- All imports use the `@/` alias — never relative paths like `../../../`
- Named exports only — no default exports for components

---

## Anti-Patterns — Never Do These

| Anti-pattern | Correct approach |
|---|---|
| Hardcoded string in JSX | `const/localization/<page>.ts` export |
| Hardcoded color (`bg-blue-600`) | Semantic token (`bg-primary`) |
| API data in Zustand | TanStack Query |
| `import axios from 'axios'` in component | Import from `@/api/courses.api` (or relevant module) |
| `EventSource` for SSE | `fetch` + `ReadableStream` |
| `IFormFile` / multipart upload | 3-step SAS URL via `UploadsController` |
| Zod schema as DTO type for backend response | Plain TypeScript interface in `types/` |
| Inline props type `{ course: CourseDto }` | Named `interface CourseCardProps` |
| Default export for a component | Named export |
| Relative import `../../..` | `@/` alias |
| `useState` for cross-component UI state | `ui.store` (Zustand) |
| Per-page guard (`<RequireAuth><Page />`) | `ProtectedRoute` layout with `<Outlet />` |
| pnpm | npm only (FADR-011) |
