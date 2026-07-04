# Learnix — Frontend Architecture Specification

> For detailed architectural decision records (ADRs) and rationale, refer to the [**decisions/**](decisions/README.md) directory.

## Overview

**Pattern:** Layer-based architecture with feature-sliced grouping inside layers  
**Framework:** React 19.2 + Vite 8  
**Language:** TypeScript 6  
**Routing:** React Router v7 (Nested layouts, lazy loading, Route Guards)  
**Type Safety:** TypeScript + Zod (Validation & Form schemas)  
**Server State:** TanStack Query (cache, refetch, mutations)  
**Client State:** Zustand (see store breakdown below)  
**HTTP Client:** Axios (Interceptor-based token refresh)  
**Forms:** React Hook Form + @hookform/resolvers  
**Realtime:** SignalR (WebSockets with automatic reconnects)  
**Internationalization:** react-i18next + zod-i18n-map  
**Styling & UI:** Tailwind CSS v3 + shadcn/ui + Lucide React + Sonner (Toasts)  
**Rich Text / Content:** uiw/react-md-editor + react-markdown  
**Drag & Drop:** @dnd-kit (for sortable lists like course lessons)  
**Fonts:** DM Sans (body) + Plus Jakarta Sans (headings) via `@fontsource` (self-hosted, no CDN)

---

## Zustand Stores

| Store | File | Persisted | Purpose |
|-------|------|-----------|---------|
| Auth | `auth.store.ts` | ❌ (in-memory only) | Access token, user summary, initialization state |
| Theme | `theme.store.ts` | ✅ `localStorage` | Light/dark mode, applies `.dark` class on `<html>` |
| Locale | `locale.store.ts` | ✅ `localStorage` | Active language (`en` / `uk`) |
| UI | `ui.store.ts` | ❌ | AI chat widget open/close state |
| Player | `player.store.ts` | ✅ `localStorage` | Video autoplay preference in the course player |

---

## Data Flow

```text
User Interaction
    ↓
Component (calls Custom Hook)
    ↓
Hook ─┬─→ [Server State] TanStack Query → Axios (API Calls)
      │
      └─→ [Client State] Zustand → Local UI/Auth State
    ↓
Backend
```
