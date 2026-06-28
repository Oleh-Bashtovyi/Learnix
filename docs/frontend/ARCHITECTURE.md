# Learnix — Frontend Architecture Specification

> For detailed architectural decision records (ADRs) and rationale, refer to the [**decisions/**](decisions/README.md) directory.

## Overview

**Pattern:** Layer-based architecture with feature-sliced grouping inside layers  
**Framework:** React 19 + Vite  
**Routing:** React Router v7 (Nested layouts, lazy loading, Route Guards)  
**Type Safety:** TypeScript + Zod (Validation & Form schemas)  
**Server State:** TanStack Query (cache, refetch, mutations)  
**Client State:** Zustand (Auth, UI, Theme)  
**HTTP Client:** Axios (Interceptor-based token refresh)  
**Forms:** React Hook Form + @hookform/resolvers  
**Realtime:** SignalR (WebSockets with automatic reconnects)  
**Internationalization:** react-i18next + zod-i18n-map  
**Styling & UI:** Tailwind CSS + shadcn/ui + Lucide React + Sonner (Toasts)  
**Rich Text / Content:** uiw/react-md-editor + react-markdown  
**Drag & Drop:** @dnd-kit (for sortable lists like course lessons)

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


