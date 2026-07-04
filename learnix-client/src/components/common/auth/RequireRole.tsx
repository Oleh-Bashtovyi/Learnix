import { Navigate, useLocation } from 'react-router-dom';
import { APP_ROUTES } from '@/routes/paths';
import { type UserSummary, useAuthStore } from '@/store/auth.store';

interface Props {
    roles: UserSummary['roles'];
    children: React.ReactNode;
}

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh (isInitializing logic)
 * - ADR-FRONT-AUTH-005: Role-Based Routing & Default Entry Points
 */
export function RequireRole({ roles, children }: Props) {
    const { user, isInitializing } = useAuthStore();
    const location = useLocation();

    if (isInitializing) return null;

    if (!user) {
        return <Navigate to={APP_ROUTES.public.login} state={{ from: location }} replace />;
    }

    const hasRole = user.roles.some((r) => roles.includes(r));
    if (!hasRole) {
        return <Navigate to={APP_ROUTES.public.home} replace />;
    }

    return <>{children}</>;
}
