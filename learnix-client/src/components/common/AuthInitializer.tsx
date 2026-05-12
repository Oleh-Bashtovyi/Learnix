import { useEffect } from 'react';
import axios from 'axios';
import { useAuthStore } from '@/store/auth.store';
import { parseAccessToken } from '@/utils/parseAccessToken';

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api';

export function AuthInitializer({ children }: { children: React.ReactNode }) {
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);
    const finishInitialization = useAuthStore((s) => s.finishInitialization);

    useEffect(() => {
        axios
            .post<{ accessToken: string }>(
                `${BASE_URL}/auth/refresh`,
                {},
                { withCredentials: true },
            )
            .then(({ data }) => {
                setAccessToken(data.accessToken);
                const user = parseAccessToken(data.accessToken);
                if (user) setUser(user);
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
