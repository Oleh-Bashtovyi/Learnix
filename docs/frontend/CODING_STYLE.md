# Learnix — Frontend Coding Style & Standards

This document defines the coding style and conventions for the React frontend. Adhering to these standards ensures consistency, maintainability, and optimal performance across the application.

## React Components

### 1. Event Handlers & Functions
- **Use `function` declarations** for local component event handlers (`function handleClick() {}`) rather than arrow functions (`const handleClick = () => {}`). 
- **Why?** `function` declarations are hoisted, allowing you to define them at the bottom of your component file. This keeps the most important part of the component (the `return` statement and hooks) at the top, improving readability.

### 2. `useCallback` and Memoization
- **Do NOT use `useCallback` blindly** for every function. 
- **Why?** `useCallback` carries overhead (memory allocation for the closure and dependency array tracking). If the function is passed to a standard DOM element (like `<button>`) or a non-memoized component, the child will re-render anyway, making `useCallback` purely a performance penalty.
- **When to use:** Only use `useCallback` when passing a function as a prop to a deeply nested, heavily optimized child component wrapped in `React.memo`, or when the function is explicitly required as a dependency in a `useEffect` to prevent infinite loops.

### 3. Component Definition & Props
- **Always use a standard function signature** with a named `interface` for props.
- **Do NOT use `React.FC` or `React.FunctionComponent`.** The React team broadly discourages `React.FC` because it breaks generic components, adds unnecessary boilerplate, and historically had issues with implicit `children`.
- Never define props inline.

```tsx
// ✅ GOOD
interface CourseCardProps {
    course: CourseDto;
    onEnroll?: (courseId: string) => void;
    isCompact?: boolean;
}

export function CourseCard({ course, onEnroll, isCompact = false }: CourseCardProps) { 
    return <div />;
}

// ❌ BAD
export function CourseCard({ course }: { course: CourseDto }) { 
    return <div />;
}
```

### 4. Exports
- Always use **named exports** for components, hooks, and utilities. Do not use `export default`, except for pages loaded via React Router's `lazy()`.
- Use the `@/` alias for all imports. Never use relative paths like `../../utils`.

## Types & Enums

### 1. Types & Interfaces
- Use `interface` instead of `type` for defining object structures (like API responses or props).
- Place API response contracts in `src/types/` with a `Dto` suffix (e.g., `export interface UserDto { ... }`).
- **Why?** Interfaces are extendable and provide better error messages in TypeScript compared to intersection types.

### 2. Enums
- Do not use TypeScript's native `enum` keyword. Instead, use **POJO (Plain Old Javascript Objects) with `as const`** and derive a union type from it.
- **Why?** Native `enum` compiles to an actual JavaScript object that can cause issues with tree-shaking, transpilers, and reverse-mapping. The POJO approach is 100% type-safe and zero-cost.

```ts
// ✅ GOOD: src/enums/lesson.enums.ts
export const LessonType = {
    Video: 'Video',
    Post: 'Post',
    Test: 'Test',
} as const;
export type LessonType = (typeof LessonType)[keyof typeof LessonType];

// ❌ BAD
export enum LessonType {
    Video = 'Video',
    Post = 'Post',
    Test = 'Test',
}
```

## State & Data Fetching

### 1. API Layer
- **Components never import axios directly.** All HTTP calls must be abstracted into typed API modules in `src/api/`.
- Use TanStack Query (`useQuery`, `useMutation`) for all server state. 

### 2. Forms
- **Zod schemas** (`src/schemas/`) are the source of truth for form state (`react-hook-form`).
- **DTOs** (`src/types/`) are the source of truth for backend contracts. Do not use Zod schemas to type backend responses.

### 3. Constants & Magic Numbers
- **No magic numbers or hardcoded validation strings.** 
- Do not hardcode numbers (like `min(1).max(255)`) or error strings inside schemas, components, or UI elements. Extract them into constants files (e.g., `src/const/auth.constants.ts` or `src/const/limits.constants.ts`).
- **Why?** Centralizing limits ensures that if a database column size changes, you only need to update the constant in one place instead of hunting down every validation schema and UI counter.

## Styling & UI

### 1. Tailwind CSS
- **100% Tailwind v3.** No SCSS or CSS Modules.
- Use **semantic tokens** (e.g., `bg-primary`, `text-muted-foreground`) instead of hardcoded colors (e.g., `bg-blue-600`).
- Always use the `cn()` utility (`@/utils/cn`) for concatenating conditional classes.
- **Mobile-First:** Use base classes for mobile screens, then apply `md:` or `lg:` for larger screens.
- **Auto-Sorting:** `prettier-plugin-tailwindcss` is used to automatically sort classes.
- **Extraction:** If a `className` becomes too long (e.g., >5 repeating classes across multiple elements), extract the UI into a reusable component.
- For rationale and theming decisions, see [decisions/UI.md](decisions/UI.md).

### 2. Safe Markdown Rendering
- **Forbidden:** Developers must not use `react-markdown` or `ReactMarkdown` directly in components. You must use our custom `MarkdownRenderer` wrapper (`@/components/common/MarkdownRenderer`).
- For rationale (XSS protection), see [decisions/UI.md — ADR-FRONT-UI-002](decisions/UI.md).

## Code Quality & Tooling

> For full rationale and alternatives, see [decisions/LINTING_FORMATTING.md](decisions/LINTING_FORMATTING.md).

### 1. Formatting (Prettier)
- **Zero Configuration:** Prettier is the sole source of truth for formatting.
- **Automated Import Sorting:** Imports are sorted on save via `@trivago/prettier-plugin-sort-imports` (React → third-party → absolute aliases (`@/`) → relative paths).
- **Tailwind Class Sorting:** `prettier-plugin-tailwindcss` enforces a consistent utility class order.

### 2. Linting (ESLint Flat Config)
- **Strict Typing:** `any` is strictly forbidden (`@typescript-eslint/no-explicit-any`).
- **Unused Imports:** `eslint-plugin-unused-imports` enforces removal of unused imports. Unused variables are an error; prefix with `_` to intentionally ignore.
- **React Strictness:** We use `eslint-plugin-react` and `eslint-plugin-react-hooks`. Never ignore `react-hooks/exhaustive-deps`. For state side-effects, prefer derived state during render over `useEffect` cascades.
- **Tailwind Validation:** `eslint-plugin-tailwindcss` is utilized to catch invalid classes. Note that `tailwindcss/enforces-shorthand` is active and will auto-fix verbose dimensions (e.g., converting `h-4 w-4` into `size-4`).

## File Naming Conventions

| Item | Convention | Example |
|---|---|---|
| React Component | PascalCase | `CourseCard.tsx` |
| React Hook | camelCase, `use` prefix | `useAuth.ts` |
| Zustand Store | camelCase, `.store.ts` suffix | `auth.store.ts` |
| API Module | camelCase, `.api.ts` suffix | `courses.api.ts` |
| Zod Schema | camelCase, `.schema.ts` suffix | `course.schema.ts` |
| Types / DTOs | camelCase, `.types.ts` suffix | `course.types.ts` |
| Utility Function | camelCase | `formatDate.ts` |
