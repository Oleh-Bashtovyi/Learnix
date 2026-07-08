import { create } from 'zustand';
import type { UserRole } from '@/enums/user.enums';

export interface UserSummary {
    id: string;
    email: string;
    fullName: string;
    roles: UserRole[];
    emailVerified: boolean;
    avatarUrl: string | null;
}

interface AuthState {
    accessToken: string | null;
    user: UserSummary | null;
    isAuthenticated: boolean;
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
    isAuthenticated: false,
    isInitializing: true,
    setAccessToken: (token) => set({ accessToken: token }),
    setUser: (user) => set({ user, isAuthenticated: !!user }),
    logout: () => set({ accessToken: null, user: null, isAuthenticated: false }),
    finishInitialization: () => set({ isInitializing: false }),
}));
