import { create } from 'zustand';

interface UiState {
    isChatOpen: boolean;
    toggleChat: () => void;
    closeChat: () => void;
}

/**
 * Related ADRs:
 * - ADR-FRONT-API-002: State Management Boundary (Client State via Zustand)
 */
export const useUiStore = create<UiState>((set) => ({
    isChatOpen: false,
    toggleChat: () => set((s) => ({ isChatOpen: !s.isChatOpen })),
    closeChat: () => set({ isChatOpen: false }),
}));
