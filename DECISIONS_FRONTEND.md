# Learnix — Frontend Architecture Decision Records

> Формат: що вирішили → чому → які альтернативи відкинули.
> Оновлюється після кожного чату, де приймались архітектурні рішення на фронті.
> Архітектурні рішення беку — в [DECISIONS.md](./DECISIONS.md).

---

## FADR-001: Layer-based структура з feature-розбивкою всередині шарів

**Рішення:** `src/` організовано по шарах (api, components, pages, hooks, store, schemas, types, utils). Feature-розбивка живе всередині кожного шару (наприклад `api/courses.api.ts`, `schemas/course.schema.ts`).

**Структура верхнього рівня:**
```
src/
├── api/             # Axios instance + endpoint modules
├── components/
│   ├── ui/          # shadcn/ui primitives
│   ├── common/      # Reusable custom components (2+ pages)
│   └── layout/      # Layout components (Header, Sidebar, Footer, *Layout)
├── pages/
│   ├── public/      # No auth required
│   ├── student/     # Role: Student
│   ├── instructor/  # Role: Instructor
│   └── admin/       # Role: Admin
├── hooks/           # Custom React hooks
├── store/           # Zustand stores
├── schemas/         # Zod schemas
├── types/           # TypeScript types (DTOs, enums)
├── utils/           # Pure utilities
├── routes/          # React Router config
└── styles/          # global.css with Tailwind + CSS variables
```

**Чому:**
- Layer-based простіший для старту — зрозумілий звідкись переносимо з React tutorials
- Feature-all-the-way (як `features/courses/{api,components,hooks}/`) краще масштабується, але для LMS з 20-30 фічами layer-based ще нормально
- Page-level компоненти живуть з самою сторінкою → не засмічують `components/common/`

**Альтернативи:**
- Pure feature-based (`features/courses/`, `features/auth/`) — краще для 50+ фіч, зайвий overhead для v1
- Pure layer-based без page-level co-location — `components/` розростеться до 100+ файлів

**Наслідки:**
- Конвенція: компонент використовується на 2+ сторінках → `components/common/`. Тільки на одній → лежить поруч зі сторінкою.

---

## FADR-002: Page co-location + правила ad-hoc компонентів

**Рішення:**
- Сторінки групуються за роллю: `pages/public/`, `pages/student/`, `pages/instructor/`, `pages/admin/`
- Кожна сторінка — окрема папка (наприклад `pages/student/CoursePlayer/`)
- Ad-hoc компоненти сторінки живуть поруч з нею:
  - **1-3 допоміжних компоненти:** flat файли в папці сторінки
  - **4+ допоміжних компоненти:** у підпапці `components/` всередині сторінки

**Приклад (few components — flat):**
```
pages/public/Landing/
├── LandingPage.tsx
├── HeroSection.tsx
├── FeaturedCourses.tsx
└── HowItWorks.tsx
```

**Приклад (many components — subfolder):**
```
pages/instructor/CourseEditor/
├── CourseEditorPage.tsx
└── components/
    ├── CourseInfoForm.tsx
    ├── SectionManager.tsx
    ├── LessonEditor.tsx
    ├── QuestionEditor.tsx
    ├── DragDropList.tsx
    └── VideoUploader.tsx
```

**Чому:**
- Ad-hoc компоненти не забруднюють `components/common/`
- Близькість до сторінки → легше знайти, легше видалити разом зі сторінкою
- Правило "1-3 flat / 4+ subfolder" тримає обидва випадки чистими

**Правило міграції:** Коли ad-hoc компонент починає використовуватись на другій сторінці — переноситься в `components/common/`.

---

## FADR-003: Роутинг — React Router v6 з nested layouts та route guards

**Рішення:**
- React Router v6 з `createBrowserRouter`
- Route guards реалізовані як **layout-компоненти** через `<Outlet />`
- Lazy loading для всіх сторінок — кожна сторінка завантажується при переході

**Структура роутера:**
```tsx
// src/routes/index.tsx
const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    children: publicRoutes,
  },
  {
    element: <ProtectedRoute requiredRole="Student" />,
    children: [
      { element: <StudentLayout />, children: studentRoutes },
    ],
  },
  {
    element: <ProtectedRoute requiredRole="Instructor" />,
    children: [
      { element: <InstructorLayout />, children: instructorRoutes },
    ],
  },
  {
    element: <ProtectedRoute requiredRole="Admin" />,
    children: [
      { element: <AdminLayout />, children: adminRoutes },
    ],
  },
]);
```

**ProtectedRoute:**
```tsx
// src/components/common/ProtectedRoute.tsx
interface ProtectedRouteProps {
  requiredRole?: UserRole;
}

export function ProtectedRoute({ requiredRole }: ProtectedRouteProps) {
  const { user, isInitializing } = useAuthStore();
  const location = useLocation();

  if (isInitializing) return <FullScreenSpinner />;

  if (!user) {
    // Запам'ятовуємо звідки прийшли, щоб повернутись після логіну
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (requiredRole && !hasRequiredRole(user.role, requiredRole)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
```

**Lazy loading:**
```tsx
// src/routes/studentRoutes.tsx
const StudentDashboard = lazy(() => import('@/pages/student/Dashboard/StudentDashboard'));
const CoursePlayerPage = lazy(() => import('@/pages/student/CoursePlayer/CoursePlayerPage'));

export const studentRoutes = [
  { path: '/dashboard', element: <Suspense fallback={<Spinner />}><StudentDashboard /></Suspense> },
  { path: '/learn/:courseId', element: <Suspense fallback={<Spinner />}><CoursePlayerPage /></Suspense> },
  // ...
];
```

**Чому:**
- Nested layouts через `<Outlet />` — стандартний підхід v6, DRY для спільних лейаутів (header, sidebar)
- Guard як layout — вся логіка перевірки доступу в одному місці
- Lazy loading — admin panel не завантажується студенту

**Альтернативи:**
- Guard в кожній сторінці (`<RequireAuth><Page /></RequireAuth>`) — дублювання
- Router data loaders (v6.4+ feature) — потужніше, але складніше для команди що не знайома з ними

---

## FADR-004: API layer — Axios instance + interceptors + queued refresh

**Рішення:**
- Один Axios instance в `src/api/axios.instance.ts`
- Request interceptor додає JWT з Zustand store
- Response interceptor ловить 401 → робить silent refresh → повторює запит
- **Черга для concurrent 401s** — якщо 5 запитів впадуть одночасно, робимо ОДИН refresh, не 5
- API модулі (`courses.api.ts`, `auth.api.ts`) — тонкі обгортки з типізованими функціями

**Axios instance:**
```ts
// src/api/axios.instance.ts
import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/auth.store';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true, // refresh token cookie автоматично відправляється
});

// Request: attach token
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Response: handle 401 with queue
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((p) => (error ? p.reject(error) : p.resolve(token!)));
  failedQueue = [];
};

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error);
    }

    // Якщо вже йде refresh — ставимо запит в чергу
    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({
          resolve: (token) => {
            original.headers.Authorization = `Bearer ${token}`;
            resolve(api(original));
          },
          reject,
        });
      });
    }

    original._retry = true;
    isRefreshing = true;

    try {
      const { data } = await axios.post(
        `${import.meta.env.VITE_API_URL}/auth/refresh`,
        {},
        { withCredentials: true }
      );
      useAuthStore.getState().setAccessToken(data.accessToken);
      processQueue(null, data.accessToken);
      original.headers.Authorization = `Bearer ${data.accessToken}`;
      return api(original);
    } catch (refreshError) {
      processQueue(refreshError, null);
      useAuthStore.getState().logout();
      window.location.href = '/login';
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);
```

**API modules — типізовані обгортки:**
```ts
// src/api/courses.api.ts
import { api } from './axios.instance';
import type { CourseDto, CreateCourseRequest, CourseFilters } from '@/types/course.types';
import type { PaginatedResult } from '@/types/api.types';

export const coursesApi = {
  getAll: (filters: CourseFilters) =>
    api.get<PaginatedResult<CourseDto>>('/courses', { params: filters }).then((r) => r.data),

  getById: (id: string) =>
    api.get<CourseDto>(`/courses/${id}`).then((r) => r.data),

  create: (data: CreateCourseRequest) =>
    api.post<CourseDto>('/courses', data).then((r) => r.data),

  update: (id: string, data: CreateCourseRequest) =>
    api.put<CourseDto>(`/courses/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    api.delete<void>(`/courses/${id}`).then((r) => r.data),
};
```

**Чому:**
- Черга 401-запитів критична: без неї 5 одночасних запитів → 5 refresh-викликів → race condition
- Типізовані API-функції дозволяють TypeScript ловити помилки контракту з беком

**Альтернативи:**
- OpenAPI codegen (openapi-typescript) — має сенс коли бек стабільний. Для проєкту де бек і фронт розробляються паралельно — ручні типи дають більше контролю
- fetch замість axios — можна, але interceptors з axios коротші і зрозуміліші
- Global fetch wrapper без axios — більше коду, менше фіч з коробки

---

## FADR-005: Server state vs client state — чіткий розподіл

**Рішення:**
- **TanStack Query** — усі дані з бекенду (курси, юзери, enrollments, нотифікації). Жодних API-даних у Zustand
- **Zustand** — тільки клієнтський стан: access token, user summary, UI стан (sidebar open, mobile menu), theme
- **useState** — локальний UI стан в межах одного компонента (open/close dropdown, form state)

**Zustand stores:**
```ts
// src/store/auth.store.ts
interface AuthState {
  accessToken: string | null;
  user: UserSummary | null;
  isInitializing: boolean;
  setAccessToken: (token: string | null) => void;
  setUser: (user: UserSummary | null) => void;
  logout: () => void;
  finishInitialization: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  isInitializing: true,
  setAccessToken: (token) => set({ accessToken: token }),
  setUser: (user) => set({ user }),
  logout: () => set({ accessToken: null, user: null }),
  finishInitialization: () => set({ isInitializing: false }),
}));
```

**React Query hooks:**
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

export function useCourse(id: string) {
  return useQuery({
    queryKey: queryKeys.courses.detail(id),
    queryFn: () => coursesApi.getById(id),
    enabled: !!id,
  });
}
```

**Правило коли йти в `ui.store` vs `useState`:**

| Стан | Де | Приклад |
|---|---|---|
| Shared між незв'язаними компонентами | `ui.store` | Sidebar open state (Header button + Sidebar body) |
| Локальний для компонента | `useState` | Dropdown open, accordion section expanded |
| Локальний для форми | `react-hook-form` | Field values, errors, touched |
| З бекенду | `React Query` | Курси, юзери, progress |

**Чому:**
- Дублювання API-даних в Zustand = складна синхронізація і stale data
- React Query з коробки: кеш, refetch, stale-while-revalidate, optimistic updates
- Zustand для auth — бо треба доступ в axios interceptors (де React-хуків немає)

**Альтернативи:**
- Redux Toolkit + RTK Query — працює, але для solo проєкту boilerplate-heavy
- Context API для auth — можливо, але гірша DX ніж Zustand (немає доступу поза React)
- Jotai / Valtio — круті, але Zustand популярніший у 2025

---

## FADR-006: Форми — Zod schemas + FormValues окремо від DTO

**Рішення:**
- Zod schema — **single source of truth** для валідації і типу **форми** (`FormValues`)
- DTO — окремий TypeScript інтерфейс в `types/`, співпадає з бекенд-контрактом
- Трансформація `FormValues → DTO` — **явна**, в `onSubmit`

**Zod schema:**
```ts
// src/schemas/course.schema.ts
import { z } from 'zod';

export const createCourseSchema = z.object({
  title: z.string().min(3, 'Title must be at least 3 characters').max(200),
  description: z.string().min(10, 'Description must be at least 10 characters'),
  price: z.number().min(0, 'Price must be >= 0'),
  categoryId: z.string().uuid(),
  tagsInput: z.string().optional(), // "react, typescript, web" — string для UX
});

export type CreateCourseFormValues = z.infer<typeof createCourseSchema>;
```

**DTO:**
```ts
// src/types/course.types.ts
export interface CreateCourseRequest {
  title: string;
  description: string;
  price: number;
  categoryId: string;
  tags: string[]; // вже масив, як очікує бекенд
}
```

**Використання у формі:**
```tsx
// src/pages/instructor/CourseEditor/CourseEditorPage.tsx
const form = useForm<CreateCourseFormValues>({
  resolver: zodResolver(createCourseSchema),
});

const createCourse = useCreateCourse();

const onSubmit = (values: CreateCourseFormValues) => {
  const request: CreateCourseRequest = {
    title: values.title,
    description: values.description,
    price: values.price,
    categoryId: values.categoryId,
    tags: values.tagsInput?.split(',').map((t) => t.trim()).filter(Boolean) ?? [],
  };
  createCourse.mutate(request);
};
```

**Чому:**
- `FormValues` і `DTO` часто розходяться: поле `tagsInput` (string для зручного вводу) ≠ `tags: string[]` (формат бекенду). Якщо робити один тип — або UX страждає, або трансформація ховається в resolver
- Zod-inference для DTO = runtime-валідація на кожен response з бекенду (overkill)
- Явна трансформація легше дебажиться і рефакториться

**Наслідки:**
- Zod схеми не використовуються для типізації response-ів з бекенду
- `types/` — ручні інтерфейси для DTO, одна папка на домен (`course.types.ts`, `user.types.ts`)

---

## FADR-007: Error handling — три рівні, ProblemDetails mapping

**Рішення:** Три рівні обробки помилок: field-level у формах, toast для бізнес-помилок, Error Boundary для crash. Єдиний шлях мапінгу `ProblemDetails → UI`.

**Рівень 1 — field-level (у формах):**
```tsx
// src/utils/errors.ts
import { AxiosError } from 'axios';

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function isValidationError(error: unknown): error is AxiosError<ProblemDetails> {
  return (
    error instanceof AxiosError &&
    error.response?.status === 400 &&
    !!error.response.data?.errors
  );
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
  if (error instanceof AxiosError) {
    const problem = error.response?.data as ProblemDetails | undefined;
    return problem?.detail ?? problem?.title ?? error.message ?? fallback;
  }
  return fallback;
}
```

```tsx
// Використання у формі
const onSubmit = async (values: CreateCourseFormValues) => {
  try {
    await createCourse.mutateAsync(request);
    toast.success('Course created');
    navigate('/instructor/courses');
  } catch (error) {
    if (isValidationError(error)) {
      const errors = error.response!.data.errors!;
      Object.entries(errors).forEach(([field, messages]) => {
        form.setError(field.charAt(0).toLowerCase() + field.slice(1) as any, {
          message: messages.join('. '),
        });
      });
    } else {
      toast.error(getErrorMessage(error));
    }
  }
};
```

**Рівень 2 — toast для бізнес-помилок:**
```tsx
// src/main.tsx
import { Toaster } from 'sonner';

<Toaster position="top-right" richColors />
```

Використовується через `toast.success()`, `toast.error()`, `toast.info()` з будь-якого місця коду.

**Глобальний handler для мутацій:**
```ts
// src/main.tsx
const queryClient = new QueryClient({
  defaultOptions: {
    mutations: {
      onError: (error) => {
        if (!isValidationError(error)) {
          toast.error(getErrorMessage(error));
        }
      },
    },
    queries: {
      staleTime: 1000 * 60,
      gcTime: 1000 * 60 * 5,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});
```

**Рівень 3 — Error Boundary для JS crashes:**
```tsx
// src/components/common/ErrorBoundary.tsx
import { Component, ReactNode } from 'react';

interface Props { children: ReactNode; fallback?: ReactNode; }
interface State { hasError: boolean; }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error('ErrorBoundary caught:', error, info);
    // Тут можна відправити в Sentry / App Insights
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback ?? <ErrorFallback />;
    }
    return this.props.children;
  }
}
```

**Мапінг статусів на UI:**

| HTTP | Що це | Де показуємо |
|---|---|---|
| 400 з `errors` | Validation з бекенду | Inline під полями форми |
| 400 без `errors` | Business error | Toast error |
| 401 | Token expired | Interceptor робить silent refresh — юзер не бачить |
| 403 | Немає прав | Toast + redirect на `/forbidden` |
| 404 | Not found | Toast або NotFoundPage (залежить від контексту) |
| 409 | Conflict | Toast error (наприклад "Already enrolled") |
| 500+ | Server error | Toast "Something went wrong" + опціонально Error Boundary |

**Чому:**
- Різні помилки потребують різної UX: validation у формі ≠ network error
- Глобальний handler в React Query знімає boilerplate з кожного handler'а
- ProblemDetails (RFC 7807) — стандарт на беку, прямо мапиться на UI

---

## FADR-008: Auth — access token в memory, silent refresh on app start

**Рішення:**
- **Access token** — в Zustand store (in-memory). Не в `localStorage` (XSS vulnerable)
- **Refresh token** — HttpOnly cookie (встановлює бек, JS не має доступу)
- **Silent refresh on app start** — при завантаженні робимо `POST /auth/refresh`, якщо cookie валідна — юзер залогінений
- **На інших вкладках** — нічого спеціального. Дізнаються через 401 → refresh → continue
- **Google OAuth** — redirect flow (не popup)

**Silent refresh:**
```tsx
// src/App.tsx
import { useEffect } from 'react';
import { useAuthStore } from '@/store/auth.store';
import { authApi } from '@/api/auth.api';

function App() {
  const { isInitializing, setAccessToken, setUser, finishInitialization } = useAuthStore();

  useEffect(() => {
    authApi.refresh()
      .then((data) => {
        setAccessToken(data.accessToken);
        setUser(data.user);
      })
      .catch(() => {
        // Cookie немає або протухла — юзер не залогінений, це нормально
      })
      .finally(() => finishInitialization());
  }, []);

  if (isInitializing) return <FullScreenSpinner />;

  return <RouterProvider router={router} />;
}
```

**Google OAuth — redirect flow:**
```tsx
// src/pages/public/Login/LoginPage.tsx
const handleGoogleLogin = () => {
  // Зберігаємо куди повернутись
  const returnUrl = location.state?.from?.pathname ?? '/dashboard';
  sessionStorage.setItem('oauth_return_url', returnUrl);
  window.location.href = `${import.meta.env.VITE_API_URL}/auth/google`;
};
```

Бек після успішного OAuth встановлює HttpOnly cookie і редіректить на `/auth/google/callback`:
```tsx
// src/pages/public/OAuthCallback/OAuthCallbackPage.tsx
useEffect(() => {
  authApi.refresh()
    .then((data) => {
      setAccessToken(data.accessToken);
      setUser(data.user);
      const returnUrl = sessionStorage.getItem('oauth_return_url') ?? '/dashboard';
      sessionStorage.removeItem('oauth_return_url');
      navigate(returnUrl, { replace: true });
    })
    .catch(() => navigate('/login?error=oauth_failed'));
}, []);
```

**Чому:**
- `localStorage` для токенів = XSS вразливість. HttpOnly cookie недоступний JS
- Silent refresh на старті — юзер не бачить flash логіну після reload сторінки
- Redirect flow простіший за popup (немає window.opener, message passing, popup blockers)
- Inter-tab sync (через `BroadcastChannel` або `storage event`) — overkill для v1

**Альтернативи:**
- Access token у `localStorage` — простіше, але небезпечно
- Popup OAuth — складніше, блокується браузерами
- Short-lived cookie для access token — можливо, але тоді треба CSRF захист

---

## FADR-009: Tailwind everywhere + shadcn/ui, CSS змінні для темної теми

**Рішення:**
- **Усі стилі через Tailwind.** Без SCSS, без CSS Modules
- **shadcn/ui** — копіюємо через CLI, працює з коробки (бо сам на Tailwind)
- **Design tokens** — в `tailwind.config.ts` (статичні: spacing, breakpoints, font sizes) + `src/index.css` (CSS variables для кольорів, бо темна тема)
- **Темна тема** — через клас `.dark` на `<html>`, керується через Zustand + localStorage
- **`cn()` helper** — для умовних класів з конфлікт-резолюшеном через `tailwind-merge`

**src/index.css — CSS змінні:**
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    --background: 210 40% 98%;
    --foreground: 222 47% 11%;
    --card: 0 0% 100%;
    --card-foreground: 222 47% 11%;
    --primary: 217 91% 60%;
    --primary-foreground: 0 0% 100%;
    --secondary: 210 40% 96%;
    --secondary-foreground: 222 47% 11%;
    --muted: 210 40% 96%;
    --muted-foreground: 215 16% 47%;
    --accent: 262 83% 58%;
    --accent-foreground: 0 0% 100%;
    --destructive: 0 72% 51%;
    --destructive-foreground: 0 0% 100%;
    --success: 160 84% 39%;
    --warning: 38 92% 50%;
    --border: 214 32% 91%;
    --input: 214 32% 91%;
    --ring: 217 91% 60%;
    --radius: 0.5rem;
  }

  .dark {
    --background: 222 47% 4%;
    --foreground: 210 40% 98%;
    --card: 222 47% 11%;
    --card-foreground: 210 40% 98%;
    --primary: 217 91% 65%;
    --primary-foreground: 0 0% 100%;
    --secondary: 217 33% 17%;
    --secondary-foreground: 210 40% 98%;
    --muted: 217 33% 17%;
    --muted-foreground: 215 20% 65%;
    --accent: 262 83% 68%;
    --accent-foreground: 0 0% 100%;
    --destructive: 0 63% 55%;
    --destructive-foreground: 0 0% 100%;
    --success: 160 84% 45%;
    --warning: 38 92% 55%;
    --border: 217 33% 20%;
    --input: 217 33% 20%;
    --ring: 217 91% 65%;
  }
}
```

**tailwind.config.ts:**
```ts
import type { Config } from 'tailwindcss';

export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    container: { center: true, padding: '1rem', screens: { '2xl': '1400px' } },
    extend: {
      colors: {
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        primary: {
          DEFAULT: 'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        success: 'hsl(var(--success))',
        warning: 'hsl(var(--warning))',
        card: {
          DEFAULT: 'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
        border: 'hsl(var(--border))',
        input: 'hsl(var(--input))',
        ring: 'hsl(var(--ring))',
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
      fontFamily: {
        sans: ['DM Sans', 'system-ui', 'sans-serif'],
        heading: ['Plus Jakarta Sans', 'DM Sans', 'sans-serif'],
      },
    },
  },
  plugins: [require('tailwindcss-animate')],
} satisfies Config;
```

**cn() helper:**
```ts
// src/utils/cn.ts
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

**Theme store:**
```ts
// src/store/theme.store.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type Theme = 'light' | 'dark';

interface ThemeState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggle: () => void;
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'light',
      setTheme: (theme) => {
        document.documentElement.classList.toggle('dark', theme === 'dark');
        set({ theme });
      },
      toggle: () => get().setTheme(get().theme === 'light' ? 'dark' : 'light'),
    }),
    { name: 'learnix-theme' }
  )
);
```

**Конвенції Tailwind:**
- Семантичні кольори: `bg-primary`, `text-foreground`, `border-border` — ніколи не hard-coded (`bg-blue-600`)
- Mobile-first: базові класи для мобільного, `md:`, `lg:` для більших екранів
- `prettier-plugin-tailwindcss` — автосортування класів (встановити одразу)
- Довгий `className` (>5 класів що повторюються) → витягти в компонент

**Чому:**
- Один ментальний контекст (Tailwind everywhere) — швидкість delivery
- shadcn/ui з коробки (бо він сам на Tailwind)
- CSS змінні в HSL формат — стандарт shadcn/ui, працює з `hsl(var(--primary) / 0.5)` для opacity
- Dark mode готовий з самого старту — як мінімум токени визначені

**Альтернативи (відкинуто):**
- SCSS Modules + shadcn hybrid — два ментальні контексти, складний setup Tailwind config під свої токени
- SCSS Modules без shadcn — тиждень-два на написання accessible примітивів
- CSS-in-JS (styled-components, emotion) — runtime overhead, не дружить з SSR

---

## FADR-010: React Query — ієрархічні query keys, optimistic для дешевих дій

**Рішення:**
- Query keys — ієрархічна структура в `src/api/queryKeys.ts`
- Дефолтні налаштування QueryClient: `staleTime: 60s`, `gcTime: 5m`, `retry: 1`, `refetchOnWindowFocus: false`
- Optimistic updates — для low-stakes дій (likes, mark complete). Pessimistic — для mutations з бізнес-наслідками (create, update, payment)
- Інвалідація — явна в `onSuccess` мутації

**queryKeys.ts:**
```ts
// src/api/queryKeys.ts
export const queryKeys = {
  courses: {
    all: ['courses'] as const,
    lists: () => [...queryKeys.courses.all, 'list'] as const,
    list: (filters: CourseFilters) => [...queryKeys.courses.lists(), filters] as const,
    details: () => [...queryKeys.courses.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.courses.details(), id] as const,
  },
  enrollments: {
    all: ['enrollments'] as const,
    byUser: (userId: string) => [...queryKeys.enrollments.all, 'user', userId] as const,
  },
  lessons: {
    all: ['lessons'] as const,
    detail: (id: string) => [...queryKeys.lessons.all, 'detail', id] as const,
    progress: (userId: string, courseId: string) =>
      [...queryKeys.lessons.all, 'progress', userId, courseId] as const,
  },
  notifications: {
    all: ['notifications'] as const,
    unread: () => [...queryKeys.notifications.all, 'unread'] as const,
  },
  // ... решта
} as const;
```

**Optimistic update (лайк):**
```ts
// src/hooks/useLikeLesson.ts
export function useLikeLesson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (lessonId: string) => lessonsApi.like(lessonId),

    onMutate: async (lessonId) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.lessons.detail(lessonId) });
      const previous = queryClient.getQueryData<LessonDto>(queryKeys.lessons.detail(lessonId));

      queryClient.setQueryData<LessonDto>(queryKeys.lessons.detail(lessonId), (old) =>
        old ? { ...old, isLiked: true, likesCount: old.likesCount + 1 } : old
      );

      return { previous };
    },

    onError: (_err, lessonId, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.lessons.detail(lessonId), context.previous);
      }
      toast.error('Failed to like lesson');
    },

    onSettled: (_data, _err, lessonId) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.lessons.detail(lessonId) });
    },
  });
}
```

**Pessimistic mutation (create course):**
```ts
// src/hooks/useCreateCourse.ts
export function useCreateCourse() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: coursesApi.create,
    onSuccess: (created) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.courses.lists() });
      queryClient.setQueryData(queryKeys.courses.detail(created.id), created);
      toast.success('Course created');
    },
    // onError — глобальний handler у QueryClient
  });
}
```

**Класифікація дій:**

| Дія | Підхід | Чому |
|---|---|---|
| Like lesson | Optimistic | Instant feedback, rollback простий |
| Mark lesson complete | Optimistic | Те саме |
| Update profile | Pessimistic | Юзер чекає підтвердження |
| Create/edit course | Pessimistic | Можливі validation errors |
| Submit test | Pessimistic | Критична операція зі score |
| Enroll (free) | Pessimistic | Створюється Enrollment на беку |
| Payment | Pessimistic | Завжди |

**Чому ієрархічні keys:**
- Одним викликом інвалідуємо "всі списки курсів" (різні фільтри) без зачіпання деталей:
  `queryClient.invalidateQueries({ queryKey: queryKeys.courses.lists() })`
- `queryKeys.courses.all` скасовує всі запити курсів разом

**Чому staleTime 60s:**
- 0 (default) — агресивний refetch на кожне mount
- Infinity — дані ніколи не оновлюються без явного invalidate
- 60s — розумний компроміс для LMS (дані не критично свіжі)

---

## FADR-011: Tooling & Libraries

**Рішення:** Фіксуємо tooling та вибір бібліотек одним записом — щоб не було дискусій "яку бібліотеку взяти" під час розробки.

### Core

| Інструмент | Вибір |
|---|---|
| Package manager | **npm 10+** (ships with Node 20) |
| Node version | **20 LTS** (зафіксовано в `.nvmrc`) |
| Bundler | Vite (йде з шаблону React + TS) |

### Code quality

| Інструмент | Конфіг |
|---|---|
| Linter | ESLint з `@typescript-eslint/recommended` + `eslint-plugin-react-hooks` + `eslint-plugin-jsx-a11y` |
| Formatter | Prettier + `prettier-plugin-tailwindcss` (автосортування Tailwind класів) |
| Git hooks | Husky + lint-staged (pre-commit: ESLint + Prettier на staged файлах) |

### UI / DX libraries

| Потреба | Бібліотека |
|---|---|
| Icons | `lucide-react` (йде з shadcn/ui) |
| Date utilities | `date-fns` |
| Drag-and-drop (reorder sections / lessons) | `@dnd-kit/core` |
| Markdown editor (Instructor, Post lesson) | `@uiw/react-md-editor` |
| Markdown renderer (Student, Post lesson view) | `react-markdown` |
| Video player (Video lessons) | Native `<video controls>` (v1 — play/pause/seekbar достатньо) |
| Toast notifications | `sonner` (через shadcn/ui) |
| Conditional classNames | `clsx` + `tailwind-merge` (через `cn()` helper) |

### File uploads

| Що | Як |
|---|---|
| Video lessons | Presigned URL → direct upload до Azure Blob |
| Course cover images | Presigned URL → direct upload до Azure Blob |
| User avatars | Presigned URL → direct upload до Azure Blob |

Бек повертає `uploadUrl` (тимчасовий SAS URL) + `blobUrl` (фінальний URL для збереження в entity). Фронт робить `PUT` файлу напряму на Azure, потім шле `blobUrl` у відповідний Command (наприклад `CreateVideoLessonCommand`).

**Чому:**
- **Зміна від першої редакції:** спочатку планувався pnpm, але на Windows він призвів до критичної несумісності з drive layout (store-dir у корені диска, EPERM на системних папках, втрата даних). Для cross-platform надійності (Win/Mac/Linux) повертаємось до npm — у v10 він достатньо швидкий, lockfile детермінований, не має платформо-специфічних квирків.
- **Node 20 LTS** — активний LTS до квітня 2026, потім maintenance LTS до 2027
- **Husky + lint-staged** — не дають закомітити код з ESLint/Prettier помилками. Врятовує від "забув запустити lint"
- **lucide-react** — бандлиться shadcn CLI, 1500+ іконок, tree-shakable
- **date-fns** — tree-shakable (імпортуєш тільки те що треба), функціональний API
- **@dnd-kit** — accessible (keyboard nav), активний проєкт, використовується в великих продуктах
- **react-markdown** + **@uiw/react-md-editor** — проста пара, без overkill від TipTap
- **Presigned URLs для upload** — файл не проходить через бек → API не буферизує 500MB відео в пам'яті

**Альтернативи (відкинуто):**

| Відкинуто | Чому |
|---|---|
| pnpm | На Windows ламається через store-dir у корені диска. Hardlinks не працюють крос-дисково. На Linux/Mac працює добре, але cross-platform надійність важливіша за економію диска. |
| yarn | Втрачає популярність у 2025 |
| Heroicons | Менший набір ніж Lucide |
| dayjs | Робочий, але date-fns функціональніший |
| react-beautiful-dnd | Не підтримується (Atlassian заморозив) |
| TipTap | Overkill для простого markdown |
| video.js / plyr | Для v1 достатньо native `<video>`, додаткові бібліотеки — коли буде реальна потреба (захист контенту, analytics) |

### Файли конфігурації

Додаються у корінь `learnix-client/`:
- `.nvmrc` — `20`
- `.eslintrc.cjs` — конфіг ESLint
- `.prettierrc` — конфіг Prettier
- `.prettierignore`, `.eslintignore`
- `.husky/` — pre-commit hook
- `package.json` → `"engines": { "node": ">=20", "npm": ">=10" }`
- `package.json` → `lint-staged` секція

### Installation cheat sheet

```bash
# Node 20 через nvm
nvm install 20
nvm use 20

# pnpm
npm install -g pnpm

# В корені learnix-client
pnpm install

# Husky одноразово
pnpm dlx husky init
```

**Наслідки:**
- Recruiter клонує репо → `nvm use` + `pnpm install` → працює без питань
- `lockfile` у репо — `pnpm-lock.yaml` (не `package-lock.json`)
- Commits з помилками ESLint/Prettier блокуються hook-ом

---

## FADR-012: Localization — static const dictionaries in `src/const/localization/`

**Рішення:** Весь UI-текст (заголовки, підписи, кнопки, FAQ, відгуки) зберігається у файлах-словниках у `src/const/localization/`. Компоненти і сторінки НЕ мають хардкоджених рядків — лише посилання на константи.

**Структура:**
```
src/const/localization/
  landingPage.ts      ← LANDING_PAGE.HERO, LANDING_PAGE.FAQ, ...
  courseCatalog.ts    ← COURSE_CATALOG.FILTERS, ...
  auth.ts             ← AUTH.LOGIN, AUTH.REGISTER, ...
  errors.ts           ← ERRORS.GENERIC, ERRORS.NOT_FOUND, ...
```

**Конвенція файлу:**
```ts
// src/const/localization/landingPage.ts
export const LANDING_PAGE = {
  FAQ: {
    heading: 'Common questions',
    items: [{ q: '...', a: '...' }, ...],
  },
  HERO: {
    badge: 'New cohorts every week',
    cta: { primary: 'Browse courses', secondary: 'Become an instructor' },
  },
  // ...
} as const;
```

**Використання в компоненті:**
```tsx
import { LANDING_PAGE } from '@/const/localization/landingPage';
const { FAQ } = LANDING_PAGE;

export function FaqSection() {
  return <h2>{FAQ.heading}</h2>;
}
```

**Чому:**
- Єдине джерело правди для UI-тексту — редагування в одному файлі без пошуку по JSX
- Готовність до i18n: заміна словника на перекладений не потребує змін у компонентах
- Текст доступний за назвою (`FAQ.heading`), а не загубленим рядком у JSX
- `as const` дає точні літеральні типи, TypeScript ловить опечатки при рефакторингу

**Альтернативи:**
- Зберігати текст у компонентах — швидко для прототипу, але погано масштабується (навіть без i18n — важко знайти і змінити всі копірайти чи дати)
- `react-i18next` або `react-intl` — потужні, але overkill для v1 без активного i18n. Конвенція з const-словниками є природним кроком перед міграцією на бібліотеку
- JSON файли з перекладами — обов'язковий крок при додаванні другої мови, але на v1 TS-файл зручніший (автокомпліт, `as const`)

**Наслідки:**
- Кожна нова сторінка пишеться з константою-словником з першого дня
- PR review помічає "новий хардкоджений рядок" як порушення конвенції
- При додаванні i18n — замінити `as const` об'єкт на виклик `t()` функції за зрозумілою схемою

---

## Шаблон для нових записів

```
## FADR-XXX: [Назва рішення]

**Рішення:** [Що саме вирішили]

**Чому:** [Обґрунтування]

**Альтернативи:** [Що розглядали і чому відкинули]

**Наслідки:** [Що це змінює в коді / архітектурі]
```
