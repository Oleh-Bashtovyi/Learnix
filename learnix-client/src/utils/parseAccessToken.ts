import type { UserSummary } from '@/store/auth.store';

interface JwtPayload {
    sub: string;
    email: string;
    name: string;
    role: string | string[];
    email_verified: string;
}

export function parseAccessToken(token: string): UserSummary | null {
    try {
        const payload = JSON.parse(atob(token.split('.')[1])) as JwtPayload;
        const role = Array.isArray(payload.role) ? payload.role[0] : payload.role;
        return {
            id: payload.sub,
            email: payload.email,
            fullName: payload.name,
            role: role as UserSummary['role'],
            emailVerified: payload.email_verified === 'true',
            avatarUrl: null,
        };
    } catch {
        return null;
    }
}
