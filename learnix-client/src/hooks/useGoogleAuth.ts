import { useMutation } from '@tanstack/react-query';
import { useNavigate, useLocation } from 'react-router-dom';
import { authApi } from '@/api/auth.api';
import { useAuthStore } from '@/store/auth.store';
import { parseAccessToken } from '@/utils/parseAccessToken';
import { getRoleHome } from '@/utils/getRoleHome';

export function useGoogleAuth() {
    const navigate = useNavigate();
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);

    const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname;

    const { mutate, isPending } = useMutation({
        mutationFn: authApi.googleLogin,
        onSuccess: (data) => {
            setAccessToken(data.accessToken);
            const user = parseAccessToken(data.accessToken);
            if (user) setUser({ ...user, avatarUrl: data.avatarUrl });
            navigate(from ?? (user ? getRoleHome(user.role) : '/courses'), { replace: true });
        },
    });

    return {
        onGoogleCredential: (credential: string) => mutate({ idToken: credential }),
        isPending,
    };
}
