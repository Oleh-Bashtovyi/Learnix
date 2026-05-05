import { create } from 'zustand';

export interface UserSummary {
    id: string;
    email: string;
    fullName: string;
    role: 'Student' | 'Instructor' | 'Admin';
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

export const useAuthStore = create<AuthState>((set) => ({
    accessToken: null,
    user: null,
    isInitializing: true,
    setAccessToken: (token) => set({ accessToken: token }),
    setUser: (user) => set({ user }),
    logout: () => set({ accessToken: null, user: null }),
    finishInitialization: () => set({ isInitializing: false }),
}));
