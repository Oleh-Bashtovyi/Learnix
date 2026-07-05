import { useLocation, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { authApi } from '@/api/auth.api';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import type { LocationStateWithFrom } from '@/types/router.types';
import { getRoleHome } from '@/utils/getRoleHome';
import { parseAccessToken } from '@/utils/parseAccessToken';

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh
 */
export function useGoogleAuth() {
    const navigate = useNavigate();
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);

    const from = (location.state as LocationStateWithFrom | null)?.from?.pathname;

    const { mutate, isPending } = useMutation({
        mutationFn: authApi.googleLogin,
        onSuccess: (data) => {
            setAccessToken(data.accessToken);
            const user = parseAccessToken(data.accessToken);
            if (user) setUser({ ...user, avatarUrl: data.avatarUrl });
            navigate(from ?? (user ? getRoleHome(user.roles) : APP_ROUTES.public.courses), {
                replace: true,
            });
        },
    });

    return {
        onGoogleCredential: (credential: string) => mutate({ idToken: credential }),
        isPending,
    };
}
