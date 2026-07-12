import type { UserSummary } from '@/store/auth.store';

interface JwtPayload {
    sub: string;
    email: string;
    name: string;
    role: string | string[];
    email_verified: string;
}

/**
 * A JWT payload is base64url — not base64 — and its bytes are UTF-8.
 *
 * `atob` handles neither: it does not know the `-`/`_` alphabet, and it returns a binary string in which
 * every byte became one Latin-1 character. Feed it a Cyrillic name and each letter's two UTF-8 bytes come
 * back as two characters — "Олена" is displayed as mojibake wherever the name is shown.
 */
function decodeJwtPayload(segment: string): unknown {
    const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const bytes = Uint8Array.from(atob(padded), (char) => char.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes));
}

export function parseAccessToken(token: string): UserSummary | null {
    try {
        const payload = decodeJwtPayload(token.split('.')[1]) as JwtPayload;
        const roles = Array.isArray(payload.role) ? payload.role : [payload.role];
        return {
            id: payload.sub,
            email: payload.email,
            fullName: payload.name,
            roles: roles as UserSummary['roles'],
            emailVerified: payload.email_verified === 'true',
            avatarUrl: null,
        };
    } catch {
        return null;
    }
}
