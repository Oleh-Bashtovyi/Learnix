import { create } from 'zustand';

interface UiState {
    isChatOpen: boolean;
    toggleChat: () => void;
    closeChat: () => void;
}

export const useUiStore = create<UiState>((set) => ({
    isChatOpen: false,
    toggleChat: () => set((s) => ({ isChatOpen: !s.isChatOpen })),
    closeChat: () => set({ isChatOpen: false }),
}));
