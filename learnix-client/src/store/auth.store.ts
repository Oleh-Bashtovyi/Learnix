import { create } from 'zustand';

export interface UserSummary {
    id: string;
    email: string;
    fullName: string;
    roles: ('Student' | 'Instructor' | 'Admin')[];
    emailVerified: boolean;
    avatarUrl: string | null;
}

interface AuthState {
    accessToken: string | null;
    user: UserSummary | null;
    isInitializing: boolean;
    setAccessToken: (token: string | null) => void;
    setUser: (user: UserSummary | null) => void;
    logout: () => void;
    finishInitialization: () => void;
}

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh
 * - ADR-FRONT-API-002: State Management Boundary (Client State via Zustand)
 */
export const useAuthStore = create<AuthState>((set) => ({
    accessToken: null,
    user: null,
    isInitializing: true,
    setAccessToken: (token) => set({ accessToken: token }),
    setUser: (user) => set({ user }),
    logout: () => set({ accessToken: null, user: null }),
    finishInitialization: () => set({ isInitializing: false }),
}));
