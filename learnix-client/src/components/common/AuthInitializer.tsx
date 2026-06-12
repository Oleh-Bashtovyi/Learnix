import { useEffect } from 'react';
import axios from 'axios';
import { useAuthStore } from '@/store/auth.store';
import { parseAccessToken } from '@/utils/parseAccessToken';
import { env } from '@/utils/env';

export function AuthInitializer({ children }: { children: React.ReactNode }) {
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);
    const finishInitialization = useAuthStore((s) => s.finishInitialization);

    useEffect(() => {
        axios
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
                finishInitialization();
            });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return <>{children}</>;
}
