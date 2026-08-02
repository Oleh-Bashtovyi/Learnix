---
name: frontend-standards
description: Frontend coding standards, pre-task checklist, folder structure, component conventions, state management rules, API layer, forms, styling, localization, error handling, and anti-patterns for the Learnix React 19 + TypeScript frontend. Use when implementing frontend features, creating components, writing hooks, adding pages, working with TanStack Query, Zustand, React Hook Form, Tailwind, or localization.
when_to_use: frontend, React, TypeScript, component, page, hook, store, Zustand, TanStack Query, React Query, form, Zod, Tailwind, shadcn, localization, axios, routing, SSE, upload
---

# Learnix Frontend Standards

## Pre-Task Checklist

Before implementing **any** frontend feature, complete these steps in order:

1. **Read `docs/TODO.md`** — identify the exact task(s), their phase, and current status.
2. **Read `docs/FEATURES.md`** — understand the functional spec for the feature being built.
3. **Read `docs/frontend/ARCHITECTURE.md`** and **`docs/frontend/PROJECT_STRUCTURE.md`** — review folder structure, component co-location rules, state split, and API layer conventions. `docs/frontend/CODING_STYLE.md` covers component design details.
4. **Check relevant ADR files in `docs/frontend/decisions/`** — read only those that apply to the current task scope (`platform/UI.md`, `platform/API.md`, `platform/AUTH.md`, `platform/FORMS.md`, `platform/I18N_SEO.md`, `platform/ARCHITECTURE.md`, `operations/LINTING_FORMATTING.md`, `operations/DEPLOYMENT.md`). The index is `docs/frontend/decisions/README.md`.
5. **Check for mockups** — look in `mockups/` for the feature. If none exists, match the visual style of existing pages.
6. **Check available backend endpoints** — read the relevant `Learnix.Backend/Learnix.API/Controllers/*.cs` files. Verify route prefix, HTTP methods, request/response shapes, and auth requirements before writing any API calls.

---

## Post-Task Checklist

After completing **any** frontend task, from `learnix-client/`:

1. `npm run type-check` — fix all TypeScript errors before finishing.
2. `npm run lint` — ESLint must pass (CI enforces it).
3. `npm run format` — Prettier + Tailwind class sort.
4. **Update `docs/TODO.md`** — set the task's `Status` column to `done` (the file uses status tables, not `[x]` checkboxes). Add a note if the implementation deviated from the spec.
5. **Update ADR files** — if a new architectural decision was made, add an entry to the appropriate file in `docs/frontend/decisions/` using `ADR-FRONT-<SCOPE>-NNN` numbering. Numbering is **scoped per file** — read the file first and take the next free number after its current highest.

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
├── hooks/            ← custom React hooks, grouped by domain:
│                       auth/ course/ instructor/ lesson/ realtime/ shared/ student/ user/
├── store/            ← Zustand stores (*.store.ts)
├── schemas/          ← Zod schemas (*.schema.ts) — source of truth for FormValues
├── types/            ← TypeScript DTO interfaces (*.types.ts)
├── enums/            ← shared enums (*.enums.ts)
├── utils/            ← pure utilities (cn.ts, errors.ts, formatDate.ts, env.ts, etc.)
├── routes/           ← React Router config (index.tsx, paths.ts)
├── i18n/             ← i18next config + locales/{en,uk}/*.json — ALL UI text
├── styles/           ← index.css (Tailwind layers + design tokens)
└── const/            ← non-text constants (*.constants.ts)
```

### Component co-location rules (ADR-FRONT-ARCH-002)

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
| Enums | camelCase, `.enums.ts` | `course.enums.ts` |
| Constants | camelCase, `.constants.ts` | `course.constants.ts` |
| Localization namespace | camelCase `.json`, in `en/` + `uk/` | `courseDetail.json` |
| Utility | camelCase | `formatDate.ts` |

---

## Styling (ADR-FRONT-UI-001)

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

## Localization (ADR-FRONT-INTL-001)

**All UI text lives in react-i18next JSON namespaces. Zero hardcoded strings in components.**

Translations: `src/i18n/locales/{en,uk}/<namespace>.json` — one namespace per page / feature area, mirrored in both languages. Config: `src/i18n/config.ts` (imported once in `main.tsx`).

```json
// src/i18n/locales/en/catalog.json
{
  "filters": { "heading": "Filters", "clear": "Clear all" },
  "empty": { "title": "No courses found", "subtitle": "Try adjusting your filters" },
  "courseCount_one": "{{count}} course",
  "courseCount_other": "{{count}} courses"
}
```

```tsx
// In component:
import { useTranslation } from 'react-i18next';

const { t } = useTranslation('catalog');
<h2>{t('filters.heading')}</h2>
<p>{t('courseCount', { count: courses.length })}</p>
```

- New page → new JSON file in **both** `en/` and `uk/`, registered in the `resources` object in `config.ts`.
- Parameterized strings → `{{variable}}` interpolation, **not** JS template functions.
- Plurals → i18next suffixes (`_one` / `_few` / `_many` / `_other`; Ukrainian follows CLDR) with a `{ count: n }` param.
- Arrays (FAQ items, testimonials) → store in JSON, read with `{ returnObjects: true }`.
- Never call `i18n.t()` directly in a component — always the `useTranslation` hook.
- Zod validation messages are localized via `zod-i18n-map` (`locales/{en,uk}/zod.json`).
- Active language lives in `locale.store.ts` (persisted to `localStorage`); `LanguageSwitcher` in the Header toggles EN/UK.
- `src/const/*.constants.ts` holds **non-text** data only (icon maps, gradients, external links) — never UI copy.

---

## State Management (ADR-FRONT-API-002)

| What | Where | Example |
|---|---|---|
| Server data (from API) | TanStack Query | courses list, user profile, enrollments |
| Auth token + user | `auth.store` (Zustand, in-memory) | `accessToken`, `user`, `isInitializing` |
| UI state shared across components | `ui.store` (Zustand) | AI chat widget open/closed |
| Theme | `theme.store` (Zustand, persisted) | `'light' \| 'dark'` |
| Active language | `locale.store` (Zustand, persisted) | `'en' \| 'uk'` |
| Video autoplay preference | `player.store` (Zustand, persisted) | course player autoplay |
| Local component state | `useState` | dropdown open, accordion expanded |
| Form values + validation | `react-hook-form` | field values, errors, touched |

**Rules:**
- Never put API data in Zustand — that's TanStack Query's job
- Use `ui.store` only when state is shared between **unrelated** components. Start with `useState`, migrate to `ui.store` if a second component needs it
- Zustand stores are accessed in axios interceptors via `useAuthStore.getState()` (not hooks)

---

## API Layer (ADR-FRONT-API-001, ADR-FRONT-API-007)

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

**React Query hook wrapping the API** — hooks live in a domain subfolder (`hooks/course/`, `hooks/instructor/`, `hooks/student/`, `hooks/user/`, `hooks/lesson/`, `hooks/auth/`, `hooks/realtime/`, `hooks/shared/`):
```ts
// src/hooks/course/useCatalogCourses.ts
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
// src/hooks/instructor/useCourseMutations.ts
export function useCreateCourse() {
  const queryClient = useQueryClient();
  const { t } = useTranslation('instructor');
  return useMutation({
    mutationFn: coursesApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.lists() });
      toast.success(t('course.created'));
    },
  });
}
```

**Query keys** are hierarchical — defined in `src/api/queryKeys.ts`. Always use them, never inline string arrays.

---

## Forms (ADR-FRONT-FORMS-002)

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

## Error Handling (ADR-FRONT-FORMS-003, ADR-FRONT-FORMS-005)

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

## Routing (ADR-FRONT-ARCH-003, ADR-FRONT-ARCH-005)

React Router **v7**.

- Route guards as **layout components** via `<Outlet />` — not per-page wrappers
- Lazy loading for every page via `React.lazy()` + `Suspense`
- `ProtectedRoute` handles: `isInitializing` spinner → auth check → role check
- **Never hardcode a path string.** Use the centralized `APP_ROUTES` dictionary in `src/routes/paths.ts`

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

AI chat streams over SSE. **Never use `EventSource`** — it can't send an `Authorization` header.

Use the shared axios instance with the **fetch adapter** so the interceptor-based token refresh still applies. The canonical implementation is `streamAiMessage()` in `src/api/aiChat.api.ts` — an async generator; consume it via `hooks/realtime/useAiChat.ts`.

```ts
const response = await api.post(
  '/ai-chat/messages',
  { message },
  { responseType: 'stream', adapter: 'fetch', signal },
);

const reader = (response.data as unknown as ReadableStream<Uint8Array>).getReader();
const decoder = new TextDecoder();
let buffer = '';

while (true) {
  const { done, value } = await reader.read();
  if (done) break;
  buffer += decoder.decode(value, { stream: true });
  // SSE events are separated by \n\n — parse complete events out of `buffer`
}
```

---

## Realtime — SignalR (ADR-FRONT-API-004)

Notifications, chat and achievements push over SignalR (`@microsoft/signalr`), **not** SSE or polling.

- One hook per hub in `hooks/realtime/`: `useNotificationsHub`, `useChatHub`, `useAchievementsHub`
- The API hub endpoint is `/hubs/notifications`
- On an incoming event, invalidate the relevant TanStack Query keys — never write server data into Zustand

---

## File Uploads

All uploads use the **3-step SAS URL flow**. Never send files directly to the backend via multipart form.

Prefer the existing `useRequestUploadUrl()` hook (`hooks/shared/`) — it wraps steps 1–2 and returns the `blobPath`:

```ts
const { uploadFile, isUploading, error } = useRequestUploadUrl();

// Steps 1 + 2: request SAS URL, then PUT the file straight to Azure Blob
const blobPath = await uploadFile('CourseCover', file);

// Step 3: send blobPath to the backend in the relevant command
await coursesApi.update(courseId, { ...data, coverBlobPath: blobPath });
```

Underneath: `uploadsApi.requestUploadUrl(target, contentType)` → `uploadsApi.uploadToBlob(uploadUrl, file)`.

Upload targets (`UploadTarget` in `api/uploads.api.ts`, mirrored by the backend validator):

| Target | Allowed content types |
|---|---|
| `Avatar` | `image/jpeg`, `image/png`, `image/webp` |
| `CourseCover` | `image/jpeg`, `image/png`, `image/webp` |
| `CategoryImage` | `image/jpeg`, `image/png`, `image/webp` |
| `LessonVideo` | `video/mp4`, `video/webm` |

Certificates are **generated server-side** (QuestPDF) — there is no certificate upload target.

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
| Hardcoded string in JSX | `t('key')` from a JSON namespace in `src/i18n/locales/` |
| `src/const/localization/*.ts` for UI text | Removed — use i18next JSON namespaces |
| Hardcoded color (`bg-blue-600`) | Semantic token (`bg-primary`) |
| API data in Zustand | TanStack Query |
| `import axios from 'axios'` in component | Import from `@/api/courses.api` (or relevant module) |
| `EventSource` for SSE | `api.post(..., { responseType: 'stream', adapter: 'fetch' })` |
| Polling for notifications | SignalR hook from `hooks/realtime/` |
| Hardcoded route string (`/instructor/courses`) | `APP_ROUTES` from `@/routes/paths` |
| `IFormFile` / multipart upload | 3-step SAS URL — use `useRequestUploadUrl()` |
| Zod schema as DTO type for backend response | Plain TypeScript interface in `types/` |
| Inline props type `{ course: CourseDto }` | Named `interface CourseCardProps` |
| Default export for a component | Named export |
| Relative import `../../..` | `@/` alias |
| `useState` for cross-component UI state | `ui.store` (Zustand) |
| Per-page guard (`<RequireAuth><Page />`) | `ProtectedRoute` layout with `<Outlet />` |
| `const handler = () => {}` for local events | `function handler() {}` for hoisting |
| Blindly using `useCallback` | Only use for memoized children or `useEffect` deps |
| pnpm | npm only |
