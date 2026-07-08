import { Navigate, useLocation } from 'react-router-dom';
import { PageFallback } from '@/components/common/system/PageFallback';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { getRoleHome } from '@/utils/getRoleHome';

interface RequireGuestProps {
    children: React.ReactElement;
}

export function RequireGuest({ children }: RequireGuestProps) {
    const { user, isInitializing } = useAuthStore();
    const location = useLocation();

    if (isInitializing) return <PageFallback />;

    if (user) {
        // Extract the original path (from) from which we arrived at login/register
        const fromObj = (location.state as { from?: string | { pathname?: string } } | null)?.from;
        const fromPath = typeof fromObj === 'string' ? fromObj : fromObj?.pathname;
        const homePath = getRoleHome(user.roles);

        // 1. REGISTRATION ISSUE: If we just registered (email is not verified)
        // and we are currently on the registration page -> redirect to /verify-email.
        // We also pass 'from' in the state so the verification page knows
        // where to redirect after the code is entered.
        if (!user.emailVerified && location.pathname === APP_ROUTES.public.register) {
            return (
                <Navigate
                    to={APP_ROUTES.public.verifyEmail}
                    replace
                    state={{ email: user.email, from: fromObj }}
                />
            );
        }

        // 2. LOGIN ISSUE: Redirect to where the user originally intended to go (fromPath).
        // If fromPath is missing, fallback to the default role page (homePath).
        return <Navigate to={fromPath ?? homePath} replace />;
    }

    return children;
}
