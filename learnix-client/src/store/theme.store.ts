import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface ThemeState {
    theme: 'light' | 'dark';
    toggleTheme: () => void;
}

/**
 * Related ADRs:
 * - ADR-FRONT-API-002: State Management Boundary (Client State via Zustand)
 */
export const useThemeStore = create<ThemeState>()(
    persist(
        (set, get) => ({
            theme: 'light',
            toggleTheme: () => {
                const next = get().theme === 'light' ? 'dark' : 'light';
                set({ theme: next });
                document.documentElement.classList.toggle('dark', next === 'dark');
            },
        }),
        {
            name: 'learnix-theme',
            onRehydrateStorage: () => (state) => {
                if (state) {
                    document.documentElement.classList.toggle('dark', state.theme === 'dark');
                }
            },
        },
    ),
);
