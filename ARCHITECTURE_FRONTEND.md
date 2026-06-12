# Learnix — Frontend Architecture

> Для архітектурних рішень і обґрунтувань — див. [DECISIONS_FRONTEND.md](./DECISIONS_FRONTEND.md).

---

## Tech Stack

| Tool | Role |
|---|---|
| React 19 + Vite | Framework & bundler |
| TypeScript | Type safety |
| React Router v6 | Routing (layout routes + lazy loading) |
| TanStack Query | Server state (cache, refetch, mutations) |
| Zustand | Client-only state (auth, UI, theme) |
| React Hook Form + Zod | Form validation; Zod schema = source of truth for form types |
| Axios | HTTP client with interceptor-based token refresh |
| Tailwind CSS | All styling (шари: base + utilities) |
| shadcn/ui | Accessible React primitives (Button, Dialog, Toast, etc.) |
| sonner | Toast notifications (via shadcn) |
| tailwind-merge + clsx | Conditional className utility (`cn()`) |

---

## Styling System

**Все на Tailwind. Без SCSS, без CSS Modules.** (див. FADR-009)

- **shadcn/ui** — примітиви генеруються через `npx shadcn-ui add`, вони стають нашим кодом у `src/components/ui/`
- **Design tokens** — CSS custom properties в `src/index.css` (для темної теми) + `tailwind.config.ts` (для статичних значень)
- **Темна тема** — через клас `.dark` на `<html>`, контролюється `theme.store` + persist в localStorage

### CSS variables (src/index.css)

Кольори в HSL форматі — вимога shadcn/ui для підтримки opacity модифікаторів типу `bg-primary/50`.

Key tokens:

| Token | Light | Dark | Purpose |
|---|---|---|---|
| `--background` | `210 40% 98%` | `222 47% 4%` | Page background |
| `--foreground` | `222 47% 11%` | `210 40% 98%` | Main text |
| `--card` | `0 0% 100%` | `222 47% 11%` | Cards, panels |
| `--primary` | `217 91% 60%` | `217 91% 65%` | Primary actions |
| `--destructive` | `0 72% 51%` | `0 63% 55%` | Errors, delete |
| `--success` | `160 84% 39%` | `160 84% 45%` | Confirmations |
| `--warning` | `38 92% 50%` | `38 92% 55%` | Warnings |
| `--accent` | `262 83% 58%` | `262 83% 68%` | Achievements, badges |
| `--muted` | `210 40% 96%` | `217 33% 17%` | Muted surfaces |
| `--muted-foreground` | `215 16% 47%` | `215 20% 65%` | Supporting text |
| `--border` | `214 32% 91%` | `217 33% 20%` | Borders |

Повний список — див. `src/index.css`. Точні значення — в FADR-009.

### Tailwind config

Підключає CSS змінні як semantic Tailwind кольори:
```ts
colors: {
  primary: {
    DEFAULT: 'hsl(var(--primary))',
    foreground: 'hsl(var(--primary-foreground))',
  },
  // ...
}
```

Далі в JSX: `<Button className="bg-primary text-primary-foreground">`.

### Fonts

- **DM Sans** — body (via `font-sans`)
- **Plus Jakarta Sans** — headings (via `font-heading`)

Підключаються через `@fontsource/dm-sans` + `@fontsource/plus-jakarta-sans` або CSS `@import` в `index.css`.

### Conventions

- Завжди семантичні токени: `bg-primary`, `text-foreground`, `border-border`. Ніколи hard-coded (`bg-blue-600`)
- Mobile-first: базові класи для мобільного, `md:`, `lg:` для більших екранів
- Умовні класи — через `cn()` з `src/utils/cn.ts`
- Автосортування — `prettier-plugin-tailwindcss` (встановити одразу)
- Довгий повторюваний `className` → витягти в reusable компонент

---

## Folder Structure

```
src/
├── api/                          # Axios instance + endpoint modules
│   ├── axios.instance.ts         # Base instance, interceptors, queued refresh
│   ├── queryKeys.ts              # Hierarchical React Query keys
│   ├── auth.api.ts
│   ├── courses.api.ts
│   ├── enrollments.api.ts
│   ├── lessons.api.ts
│   ├── tests.api.ts
│   ├── payments.api.ts
│   ├── users.api.ts
│   ├── chat.api.ts
│   ├── reviews.api.ts
│   ├── notifications.api.ts
│   └── admin.api.ts
│
├── assets/
│   ├── images/
│   ├── icons/
│   └── fonts/
│
├── components/
│   ├── ui/                       # shadcn/ui primitives (generated)
│   │   ├── button.tsx
│   │   ├── dialog.tsx
│   │   ├── input.tsx
│   │   └── ...
│   ├── common/                   # Reusable custom components (used on 2+ pages)
│   │   ├── Logo.tsx
│   │   ├── CourseCard.tsx
│   │   ├── LessonItem.tsx
│   │   ├── AchievementBadge.tsx
│   │   ├── RatingStars.tsx
│   │   ├── SearchBar.tsx
│   │   ├── Pagination.tsx
│   │   ├── EmptyState.tsx
│   │   ├── LoadingSpinner.tsx
│   │   ├── FullScreenSpinner.tsx
│   │   ├── ConfirmDialog.tsx
│   │   ├── StatusBadge.tsx
│   │   ├── MarkdownRenderer.tsx
│   │   ├── VideoPlayer.tsx
│   │   ├── ThemeToggle.tsx
│   │   ├── ProtectedRoute.tsx
│   │   ├── ErrorBoundary.tsx
│   │   └── ErrorFallback.tsx
│   └── layout/
│       ├── PublicLayout.tsx
│       ├── StudentLayout.tsx
│       ├── InstructorLayout.tsx
│       ├── AdminLayout.tsx
│       ├── Header.tsx
│       ├── Footer.tsx
│       ├── Sidebar.tsx
│       └── NotificationBell.tsx
│
├── pages/
│   ├── public/                   # No auth required
│   │   ├── Landing/
│   │   │   ├── LandingPage.tsx
│   │   │   ├── HeroSection.tsx         # ad-hoc (1-3 components: flat)
│   │   │   ├── FeaturedCourses.tsx
│   │   │   └── HowItWorks.tsx
│   │   ├── CourseCatalog/
│   │   │   ├── CourseCatalogPage.tsx
│   │   │   ├── FilterSidebar.tsx
│   │   │   └── SortDropdown.tsx
│   │   ├── CourseDetail/
│   │   │   ├── CourseDetailPage.tsx
│   │   │   ├── CourseHeader.tsx
│   │   │   ├── CurriculumAccordion.tsx
│   │   │   ├── InstructorInfo.tsx
│   │   │   └── ReviewsList.tsx
│   │   ├── Login/
│   │   ├── Register/
│   │   ├── ForgotPassword/
│   │   ├── ResetPassword/
│   │   ├── VerifyEmail/
│   │   ├── OAuthCallback/
│   │   ├── Faq/
│   │   └── NotFound/
│   │
│   ├── student/                  # Role: Student (or higher)
│   │   ├── Dashboard/
│   │   ├── CoursePlayer/
│   │   │   ├── CoursePlayerPage.tsx
│   │   │   └── components/             # 4+ components: subfolder
│   │   │       ├── LessonSidebar.tsx
│   │   │       ├── VideoLessonView.tsx
│   │   │       ├── PostLessonView.tsx
│   │   │       ├── TestLessonView.tsx
│   │   │       └── LessonNavigation.tsx
│   │   ├── Profile/
│   │   ├── Achievements/
│   │   ├── Certificates/
│   │   ├── Messages/
│   │   └── BecomeInstructor/
│   │
│   ├── instructor/               # Role: Instructor
│   │   ├── Dashboard/
│   │   ├── MyCourses/
│   │   ├── CourseEditor/
│   │   │   ├── CourseEditorPage.tsx
│   │   │   └── components/             # 4+ components: subfolder
│   │   │       ├── CourseInfoForm.tsx
│   │   │       ├── SectionManager.tsx
│   │   │       ├── LessonEditor.tsx
│   │   │       ├── QuestionEditor.tsx
│   │   │       ├── DragDropList.tsx
│   │   │       └── VideoUploader.tsx
│   │   └── Messages/
│   │
│   └── admin/                    # Role: Admin
│       ├── Dashboard/
│       ├── UserManagement/
│       ├── CourseModeration/
│       ├── InstructorApplications/
│       ├── PaymentHistory/
│       └── PlatformLogs/
│
├── hooks/                        # Custom React hooks
│   ├── useAuth.ts
│   ├── useDebounce.ts
│   ├── useMediaQuery.ts
│   ├── useClickOutside.ts
│   ├── useCourses.ts             # React Query hooks
│   ├── useCourse.ts
│   ├── useCreateCourse.ts
│   ├── useEnrollments.ts
│   └── ...
│
├── store/                        # Zustand stores
│   ├── auth.store.ts             # accessToken, user, isInitializing
│   ├── theme.store.ts            # light/dark, persisted
│   ├── ui.store.ts               # isSidebarOpen, isMobileMenuOpen, isChatOpen
│   └── locale.store.ts           # language ('en'|'uk'), persisted, syncs i18n.changeLanguage
│
├── i18n/
│   ├── config.ts                 # i18next init, resource registration (all 20 namespaces × 2 langs)
│   └── locales/
│       ├── en/                   # English JSON files (one per namespace)
│       └── uk/                   # Ukrainian JSON files (mirror of en/)
│
├── const/
│   ├── localization/
│   │   └── achievements.ts       # icon + gradient data only (text moved to i18n/locales)
│   ├── auth.constants.ts         # AUTH_LIMITS — field lengths for auth forms
│   ├── course.constants.ts       # COURSE_LIMITS — title/description/tag constraints
│   ├── instructor.constants.ts   # INSTRUCTOR_APP_LIMITS — motivation text, portfolio URL
│   ├── lesson.constants.ts       # LESSON_LIMITS — title, content, test thresholds, question counts
│   ├── payment.constants.ts      # PAYMENT_LIMITS — card number, CVV, expiry lengths
│   ├── profile.constants.ts      # PROFILE_LIMITS — name and bio lengths
│   ├── review.constants.ts       # REVIEW_LIMITS — rating range, comment length
│   └── ui.constants.ts           # PAGINATION (page sizes per context), CHAT_LIMITS
│
├── schemas/                      # Zod schemas (source of truth for FormValues)
│   ├── auth.schema.ts
│   ├── course.schema.ts
│   ├── lesson.schema.ts
│   ├── review.schema.ts
│   └── profile.schema.ts
│
├── types/                        # TypeScript types (DTOs, enums)
│   ├── api.types.ts              # ProblemDetails, PaginatedResult, generic wrappers
│   ├── enums.ts                  # Mirror of backend enums
│   ├── auth.types.ts             # LoginRequest, TokenResponse, UserSummary
│   ├── course.types.ts           # CourseDto, CreateCourseRequest, CourseFilters
│   ├── lesson.types.ts
│   ├── user.types.ts
│   ├── payment.types.ts
│   └── ...
│
├── utils/
│   ├── cn.ts                     # clsx + tailwind-merge
│   ├── errors.ts                 # isValidationError, getErrorMessage, ProblemDetails
│   ├── env.ts                    # Typed env accessor
│   ├── formatDate.ts
│   ├── formatPrice.ts
│   └── pluralize.ts
│
├── routes/
│   ├── index.tsx                 # createBrowserRouter definition
│   ├── publicRoutes.tsx
│   ├── studentRoutes.tsx
│   ├── instructorRoutes.tsx
│   └── adminRoutes.tsx
│
├── styles/
│   └── index.css                 # Tailwind layers + CSS variables (light/dark)
│
├── App.tsx                       # RouterProvider + silent refresh on start
├── main.tsx                      # ReactDOM + QueryClient + Toaster + ErrorBoundary
└── vite-env.d.ts                 # Typed import.meta.env
```

### Key structural rules (FADR-001, FADR-002)

1. **`components/ui/`** — тільки shadcn/ui. Генеруємо через `npx shadcn-ui add <component>`. Вручну файли тут не створюємо.
2. **`components/common/`** — компоненти що використовуються на 2+ сторінках.
3. **`components/layout/`** — layouts і великі layout-частини (Header, Sidebar).
4. **Ad-hoc компоненти сторінки:**
   - 1-3 додаткові компоненти → flat файли поруч зі сторінкою
   - 4+ → у підпапці `components/` всередині сторінки
5. **Правило міграції:** компонент використовується на другій сторінці → переноситься в `components/common/`.
6. **`const/` — константи:** усі magic numbers (мін/макс довжини полів, діапазони значень, розміри сторінок) виносяться у відповідний файл `const/*.constants.ts`. Схеми Zod і компоненти імпортують звідти — не хардкодять числа.

---

## Pages & Routes

### Public (no auth)

| Route | Page | Description |
|---|---|---|
| `/` | `LandingPage` | Hero, featured courses, how it works, CTA |
| `/courses` | `CourseCatalogPage` | Browse with search, filters, sort, pagination |
| `/courses/:slug` | `CourseDetailPage` | Description, curriculum, instructor, reviews, enroll |
| `/login` | `LoginPage` | Email + password, Google OAuth button |
| `/register` | `RegisterPage` | Form + Google OAuth |
| `/forgot-password` | `ForgotPasswordPage` | Email input, sends reset link |
| `/reset-password` | `ResetPasswordPage` | New password form (token from URL) |
| `/verify-email` | `VerifyEmailPage` | Confirm token, show status |
| `/auth/google/callback` | `OAuthCallbackPage` | Handle OAuth redirect, refresh, navigate |
| `/faq` | `FaqPage` | Accordion with common questions |
| `*` | `NotFoundPage` | 404 |

### Student (requires `Student` role or higher)

| Route | Page | Description |
|---|---|---|
| `/dashboard` | `StudentDashboard` | Enrolled courses progress, recommendations |
| `/learn/:courseId` | `CoursePlayerPage` | Lesson viewer: video/post/test + sidebar |
| `/profile` | `ProfilePage` | Edit name, avatar, bio, category preferences |
| `/achievements` | `AchievementsPage` | Grid of earned + locked achievements |
| `/certificates` | `CertificatesPage` | List of earned certificates, download PDF |
| `/messages` | `MessagesPage` | Conversations with instructors (per course) |
| `/become-instructor` | `BecomeInstructorPage` | Application form |

### Instructor (requires `Instructor` role)

| Route | Page | Description |
|---|---|---|
| `/instructor` | `InstructorDashboard` | Stats, enrollment chart, course list |
| `/instructor/courses` | `MyCoursesPage` | CRUD list of own courses |
| `/instructor/courses/new` | `CourseEditorPage` | Create new course |
| `/instructor/courses/:id/edit` | `CourseEditorPage` | Edit course: info, sections, lessons, tests |
| `/instructor/messages` | `InstructorMessagesPage` | Reply to student messages |

### Admin (requires `Admin` role)

| Route | Page | Description |
|---|---|---|
| `/admin` | `AdminDashboard` | Platform overview |
| `/admin/users` | `UserManagementPage` | Table with search, filter by role, ban/unban |
| `/admin/courses` | `CourseModerationPage` | All courses, unpublish/delete |
| `/admin/applications` | `InstructorApplicationsPage` | Pending applications, approve/reject |
| `/admin/payments` | `PaymentHistoryPage` | All payments with status filter |
| `/admin/logs` | `PlatformLogsPage` | Error and action logs |

### Route protection (FADR-003)

`ProtectedRoute` — layout-компонент через `<Outlet />`. Перевіряє:
1. Чи завершилась ініціалізація auth (silent refresh)
2. Чи юзер залогінений — якщо ні, редірект на `/login` з `state.from` для повернення
3. Чи має потрібну роль — якщо ні, редірект на `/dashboard`

Lazy loading — кожна сторінка завантажується при переході через `React.lazy()` + `Suspense`.

---

## Component Conventions

### Props — завжди через named interface

```tsx
// OK
interface CourseCardProps {
  course: CourseDto;
  onEnroll?: (courseId: string) => void;
  isCompact?: boolean;
}

export function CourseCard({ course, onEnroll, isCompact = false }: CourseCardProps) {
  // ...
}

// Ні — inline props
export function CourseCard({ course }: { course: CourseDto }) {}
```

### File naming

| Item | Convention | Example |
|---|---|---|
| Component | PascalCase | `CourseCard.tsx` |
| Hook | camelCase, `use` prefix | `useAuth.ts` |
| Store | camelCase, `.store.ts` suffix | `auth.store.ts` |
| API module | camelCase, `.api.ts` suffix | `courses.api.ts` |
| Schema | camelCase, `.schema.ts` suffix | `course.schema.ts` |
| Types | camelCase, `.types.ts` suffix | `course.types.ts` |
| Utility | camelCase | `formatDate.ts` |

### Styling component приклад

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
        className
      )}
    >
      <h3 className="font-heading text-lg font-semibold text-foreground">
        {course.title}
      </h3>
      <p className="mt-2 text-sm text-muted-foreground">
        {course.description}
      </p>
    </div>
  );
}
```

---

## State Management (FADR-005)

### Server state — TanStack Query

Усі дані з API проходять через React Query. Ніяких API-даних в Zustand.

### Client state — Zustand

| Store | Data |
|---|---|
| `auth.store` | `accessToken` (in-memory), `user`, `isInitializing` |
| `theme.store` | `'light' \| 'dark'`, persisted в localStorage |
| `ui.store` | `isSidebarOpen`, `isMobileMenuOpen`, `isChatOpen` |
| `locale.store` | `'en' \| 'uk'`, persisted в localStorage, синхронізує `i18n.changeLanguage` |

### `ui.store` vs `useState` — правило

- **`ui.store`** тільки коли стан шариться між непов'язаними компонентами (sidebar — toggle в Header, body в Sidebar)
- **`useState`** для всього локального (dropdown open, accordion expanded, modal у формі)
- Якщо почалось з `useState` і потім потрібен в іншому компоненті — мігруй в `ui.store`

---

## API Layer & Token Refresh (FADR-004)

### Flow

```
┌──────────┐    request    ┌──────────────┐    HTTP    ┌──────────┐
│Component │  ──────────►  │ Axios        │  ───────►  │ Backend  │
│(via hook)│               │ Instance     │            │ API      │
└──────────┘               │              │            └────┬─────┘
                           │ Interceptors │                 │
                           │  ├─ request: attach JWT        │
                           │  └─ response: handle 401  ◄────┘
                           └──────────────┘
                                  │ 401?
                                  ▼
                           POST /auth/refresh
                           (cookie-based, automatic)
                                  │
                           ┌──────┴──────┐
                           │ Success     │ Failure
                           │ → retry     │ → logout
                           │   original  │   redirect
                           └─────────────┘
```

### Ключові поведінки

- **Access token** зберігається в Zustand (in-memory). Додається request interceptor'ом як `Authorization: Bearer <token>`
- **Refresh token** — HttpOnly cookie, браузер відправляє автоматично
- На **401 response** interceptor викликає `POST /auth/refresh`. Успіх → original request retry з новим token. Невдача → clear auth store + redirect на `/login`
- **Queue для concurrent 401s**: поки йде refresh, інші падаючі запити стають у чергу і повторюються після refresh. Запобігає multiple refresh calls
- API модулі (`courses.api.ts`, etc.) імпортують спільний instance і експортують типізовані функції. Компоненти **ніколи** не імпортують axios напряму

---

## React Query setup (FADR-010)

### QueryClient defaults

```ts
// src/main.tsx
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60,          // 1 min
      gcTime: 1000 * 60 * 5,         // 5 min
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: {
      onError: (error) => {
        if (!isValidationError(error)) {
          toast.error(getErrorMessage(error));
        }
      },
    },
  },
});
```

### Query keys

Ієрархічна структура в `src/api/queryKeys.ts` — дозволяє інвалідувати цілі групи queries одним викликом. Детально — FADR-010.

### DevTools

У dev-режимі обов'язково:
```tsx
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

{import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
```

---

## Error Handling (FADR-007)

Три рівні:

1. **Field-level** — у формах: мапимо `ProblemDetails.errors` → `form.setError` для кожного поля
2. **Toast** — бізнес-помилки (401 вже оброблено interceptor'ом, інші йдуть через глобальний `mutations.onError`)
3. **Error Boundary** — JS crashes, не HTTP. Обгортає root + може бути навколо окремих сторінок

Утиліти в `src/utils/errors.ts`:
- `isValidationError(error)` — type guard для 400 з `errors`
- `getErrorMessage(error, fallback?)` — витягує читабельне повідомлення з AxiosError

---

## Typed Environment Variables

Всі `VITE_*` змінні типізуються через `vite-env.d.ts`:

```ts
// src/vite-env.d.ts
/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  readonly VITE_GOOGLE_CLIENT_ID: string;
  readonly VITE_STRIPE_PUBLISHABLE_KEY: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
```

Доступ через type-safe wrapper:

```ts
// src/utils/env.ts
export const env = {
  API_URL: import.meta.env.VITE_API_URL,
  GOOGLE_CLIENT_ID: import.meta.env.VITE_GOOGLE_CLIENT_ID,
  STRIPE_PUBLISHABLE_KEY: import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY,
} as const;

// Fail fast at startup if required var is missing
Object.entries(env).forEach(([key, value]) => {
  if (!value) throw new Error(`Missing env variable: VITE_${key}`);
});
```

`.env.example` тримаємо в корені `learnix-client/` з усіма `VITE_*` ключами без значень.

---

## Vite Aliases

```ts
// vite.config.ts
resolve: {
  alias: {
    '@': path.resolve(__dirname, './src'),
  },
}
```

```json
// tsconfig.json
{
  "compilerOptions": {
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

Всі імпорти через `@/`:
```tsx
import { CourseCard } from '@/components/common/CourseCard';
import { useCourses } from '@/hooks/useCourses';
import { cn } from '@/utils/cn';
```

---

## Zod → FormValues → DTO (FADR-006)

Zod схема — source of truth для **форми**. DTO — окремий інтерфейс у `types/`. Трансформація між ними — явна в `onSubmit`.

```ts
// src/schemas/course.schema.ts
export const createCourseSchema = z.object({
  title: z.string().min(3).max(200),
  description: z.string().min(10),
  price: z.number().min(0),
  categoryId: z.string().uuid(),
  tagsInput: z.string().optional(),
});

export type CreateCourseFormValues = z.infer<typeof createCourseSchema>;
```

```ts
// src/types/course.types.ts
export interface CreateCourseRequest {
  title: string;
  description: string;
  price: number;
  categoryId: string;
  tags: string[];
}
```

Zod **не** використовується для типізації response-ів з бекенду.

---

## AI Chat Widget

- Плаваюча кнопка внизу справа на всіх authenticated сторінках
- Відкриває панель з історією розмови
- Повідомлення через `POST /ai/chat`, response — streamed via **SSE** (`EventSource` або `fetch` з `ReadableStream`)
- Історія підтягується з MongoDB на mount
- UI state (open/closed) — в `ui.store`

---

## Summary of Decisions

| Decision | Choice | ADR |
|---|---|---|
| Project structure | Layer-based with feature-split inside layers | FADR-001 |
| Page scoping | Co-located with page (flat/subfolder rule) | FADR-002 |
| Routing | React Router v6, nested layouts, layout-based guards | FADR-003 |
| HTTP client | Axios + interceptors + queued refresh | FADR-004 |
| Server state | TanStack Query (no API data in Zustand) | FADR-005 |
| Client state | Zustand (auth, theme, ui, locale) | FADR-005 |
| Forms | Zod schema → FormValues; DTO separate | FADR-006 |
| Error handling | ProblemDetails → field/toast/boundary | FADR-007 |
| Auth | Access in memory, refresh in HttpOnly cookie, silent refresh on start | FADR-008 |
| Styling | Tailwind everywhere + shadcn/ui, CSS vars for theme | FADR-009 |
| Query keys | Hierarchical; optimistic only for low-stakes | FADR-010 |
| Localization | react-i18next, JSON namespaces, EN+UK, LanguageSwitcher in Header | FADR-012 |
