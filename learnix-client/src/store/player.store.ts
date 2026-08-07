import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface PlayerState {
    autoplay: boolean;
    toggleAutoplay: () => void;
}

export const usePlayerStore = create<PlayerState>()(
    persist(
        (set) => ({
            autoplay: true,
            toggleAutoplay: () => set((state) => ({ autoplay: !state.autoplay })),
        }),
        {
            name: 'player-store',
        },
    ),
);
