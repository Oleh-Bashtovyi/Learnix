import { useEffect } from 'react';
import axios from 'axios';
import { useAuthStore } from '@/store/auth.store';
import { env } from '@/utils/env';
import { parseAccessToken } from '@/utils/parseAccessToken';

type AuthInitializerProps = {
    children: React.ReactNode;
};

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh
 */
let refreshPromise: Promise<void> | null = null;

export function AuthInitializer({ children }: AuthInitializerProps) {
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);
    const finishInitialization = useAuthStore((s) => s.finishInitialization);

    useEffect(() => {
        if (!refreshPromise) {
            refreshPromise = axios
                .post<{ accessToken: string; avatarUrl: string | null }>(
                    `${env.API_URL}/auth/refresh`,
                    {},
                    { withCredentials: true },
                )
                .then(({ data }) => {
                    setAccessToken(data.accessToken);
                    const user = parseAccessToken(data.accessToken);
                    if (user) setUser({ ...user, avatarUrl: data.avatarUrl });
                })
                .catch(() => {
                    // No valid refresh token — user is not logged in
                })
                .finally(() => {
                    refreshPromise = null;
                });
        } else {
            return;
        }

        refreshPromise.finally(() => {
            finishInitialization();
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return <>{children}</>;
}
