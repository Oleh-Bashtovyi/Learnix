import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/** One-time hints, by id. Add a value here and the store will remember it was shown. */
export const ONBOARDING_HINTS = {
    /** Points at the assistant buttons in the course player header. */
    coursePlayerAssistant: 'coursePlayerAssistant',
} as const;

export type OnboardingHint = (typeof ONBOARDING_HINTS)[keyof typeof ONBOARDING_HINTS];

interface OnboardingState {
    dismissed: Partial<Record<OnboardingHint, boolean>>;
    isDismissed: (hint: OnboardingHint) => boolean;
    dismiss: (hint: OnboardingHint) => void;
}

/**
 * Which one-time hints this person has already seen. Persisted with no expiry: a hint that comes back
 * is no longer a hint, it is a nag, and being told the same thing twice is how a product teaches you
 * to ignore it.
 *
 * A map keyed by hint id, rather than a boolean per hint, so the next hint costs one entry in
 * ONBOARDING_HINTS and nothing else — including no migration of what is already in localStorage.
 *
 * Related ADRs:
 * - ADR-FRONT-API-002: State Management Boundary (Client State via Zustand)
 */
export const useOnboardingStore = create<OnboardingState>()(
    persist(
        (set, get) => ({
            dismissed: {},
            isDismissed: (hint) => get().dismissed[hint] === true,
            dismiss: (hint) =>
                set((state) => ({ dismissed: { ...state.dismissed, [hint]: true } })),
        }),
        {
            name: 'onboarding-store',
        },
    ),
);
