import axios from 'axios';
import type { UserSummary } from '@/store/auth.store';
import { useAuthStore } from '@/store/auth.store';
import { env } from '@/utils/env';
import { parseAccessToken } from '@/utils/parseAccessToken';

interface RefreshResponse {
    accessToken: string;
    avatarUrl: string | null;
}

/**
 * Exchanges the HttpOnly refresh cookie for a fresh access token and syncs the auth store — the token,
 * plus the user (roles + `emailVerified`) re-derived from its claims.
 *
 * `RefreshTokenCommandHandler` re-reads roles from the database, so this is also how a role change takes
 * effect on the client mid-session (ADR-BACK-NOTIF-002): nothing revokes the JWT when an admin grants or
 * revokes a role, so the store keeps the old roles until the token is swapped here.
 *
 * Uses bare `axios`, not the `api` instance, to stay clear of the 401 refresh interceptor. Resolves to
 * the parsed user, or `null` when there is no valid refresh cookie (i.e. the caller is not signed in);
 * rejects on network errors so callers can decide whether to swallow them.
 *
 * Related ADRs:
 * - ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh
 * - ADR-FRONT-AUTH-006: Mid-Session Role Change Forces a Token Refresh
 */
export async function refreshSession(): Promise<UserSummary | null> {
    const { data } = await axios.post<RefreshResponse>(
        `${env.API_URL}/auth/refresh`,
        {},
        { withCredentials: true },
    );

    const { setAccessToken, setUser } = useAuthStore.getState();
    setAccessToken(data.accessToken);

    const user = parseAccessToken(data.accessToken);
    if (user) setUser({ ...user, avatarUrl: data.avatarUrl });

    return user;
}
