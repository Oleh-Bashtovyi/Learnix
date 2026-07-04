# Learnix — Frontend Project Structure

This document outlines the standard directory layout and component placement conventions for the Learnix client application. It serves as the source of truth for where specific modules, pages, and logical units reside.

## Detailed Folder Structure

```text
src/
├── api/                          # Axios instance + endpoint modules
│   ├── axios.instance.ts         # Base instance, interceptors, queued refresh
│   ├── queryKeys.ts              # Hierarchical React Query keys
│   └── *.api.ts                  # Domain-specific API wrappers
│
├── assets/                       # Static assets
│   └── achievements/             # Gamification badge images
│
├── components/                   # React components
│   ├── common/                   # Shared components (used on 2+ pages)
│   │   ├── AiChatWidget/         # AI assistant integration
│   │   ├── auth/                 # Login/Register shared components
│   │   ├── course/               # CourseCard, Rating, etc.
│   │   ├── form/                 # React Hook Form wrappers (Input, Select)
│   │   ├── icons/                # Custom SVG icons
│   │   ├── messaging/            # Real-time chat UI components
│   │   ├── system/               # Theme toggle, language switcher
│   │   └── ui/                   # shadcn/ui primitives (Button, Dialog, etc.)
│   └── layout/                   # Layouts (Header, Footer, Sidebar, RootLayout)
│
├── const/                        # Constants (limits, hardcoded values)
├── enums/                        # TypeScript enums mapped from backend
├── hooks/                        # Custom React Query and utility hooks
│   ├── auth/                     # Authentication hooks
│   ├── course/                   # Course loading & mutation hooks
│   ├── instructor/               # Hooks specific to instructor stats/editor
│   ├── lesson/                   # Lesson & test execution hooks
│   ├── realtime/                 # SignalR hub connections (chat)
│   ├── shared/                   # Generic utilities (useDebounce, usePagination)
│   ├── student/                  # Student enrollments & achievements
│   └── user/                     # Profile & settings hooks
│
├── i18n/                         # react-i18next config & locales
│   └── locales/
│       ├── en/                   # English translation namespaces
│       └── uk/                   # Ukrainian translation namespaces
│
├── pages/                        # Page-level components organized by role
│   ├── admin/                    # Role: Admin
│   │   ├── Categories/           # Category management
│   │   │   └── components/       # (e.g., CategoryRow, CategoryCreateRow)
│   │   ├── CourseModeration/     # Approving/rejecting courses
│   │   │   └── components/       # (e.g., CourseModerationTableRow)
│   │   ├── Dashboard/            # Admin metrics and overview
│   │   ├── InstructorApplications/ # Reviewing instructor requests
│   │   ├── PaymentHistory/       # Global payment records
│   │   │   └── components/       # (e.g., PaymentHistoryRow)
│   │   └── UserManagement/       # Managing all users (ban, roles)
│   │       └── components/       # (e.g., UserTableRow)
│   │
│   ├── instructor/               # Role: Instructor
│   │   ├── CourseEditor/         # Full course builder (lessons, tests, questions)
│   │   ├── Dashboard/            # Instructor stats
│   │   ├── Earnings/             # Revenue tracking
│   │   │   └── components/       # (e.g., InstructorEarningsRow)
│   │   ├── Messages/             # Shares the student/Messages/ page (same route: /messages)
│   │   └── MyCourses/            # List of created courses
│   │       └── components/       # (e.g., InstructorCourseRow)
│   │
│   ├── public/                   # Publicly accessible routes (No auth required)
│   │   ├── BecomeInstructor/     # Landing for potential instructors
│   │   ├── CourseCatalog/        # Browsing all courses
│   │   ├── CourseDetail/         # Single course view
│   │   ├── Faq/                  # Frequently asked questions
│   │   ├── InstructorProfile/    # Public instructor profile page
│   │   ├── Landing/              # Home page
│   │   ├── Login, Register, ForgotPassword, ResetPassword, VerifyEmail # Auth flows
│   │   ├── NotFound/             # 404 error page
│   │   └── VerifyCertificate/    # Public certificate verification
│   │
│   └── student/                  # Role: Student
│       ├── Achievements/         # Gamification badges display
│       ├── Certificates/         # Earned course certificates
│       ├── CoursePlayer/         # Video and text lesson viewer
│       ├── Messages/             # Chat with instructors
│       ├── MyLearning/           # Enrolled courses
│       ├── Notifications/        # In-app alerts
│       ├── Payment/              # Checkout flow (Stripe mock)
│       ├── Profile/              # User settings
│       ├── TestLesson/           # Quiz execution interface
│       └── Wishlist/             # Saved courses
│
├── routes/                       # React Router v7 configuration (index.tsx, guards)
├── schemas/                      # Zod validation schemas (Form validation)
├── store/                        # Zustand global state (auth, ui)
├── styles/                       # CSS (Tailwind base + CSS variables)
├── types/                        # TypeScript types (DTOs, domain interfaces)
└── utils/                        # Pure utility functions
    └── mocks/                    # Mock data for testing and placeholders
```

## Component Organization Rules

1. **`components/ui/`** — Tightly controlled directory for shadcn/ui primitives. Generated via `npx shadcn-ui add <component>`. Avoid creating ad-hoc files here.
2. **`components/common/`** — Shared components that are used across **2 or more pages** (e.g., `CourseCard`, `Pagination`). Organized by domain subfolders (e.g., `course/`, `form/`).
3. **`components/layout/`** — Broad structure components (e.g., `Header`, `PublicLayout`).
4. **Ad-hoc Components (Page-Level Co-location):**
   - Each page folder (e.g., `pages/student/CoursePlayer/`) can contain its own `components/` or `hooks/` subfolders for logic strictly tied to that page.
   - For example, `TestLesson` has its own `components/questions/` for rendering specific question types. Do not pollute global folders with page-specific components.
5. **Migration Rule:** When a page-level component is needed on a second page, move it to the appropriate subfolder in `components/common/`.
