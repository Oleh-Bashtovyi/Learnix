import { Navigate, useLocation } from 'react-router-dom';
import { type UserSummary, useAuthStore } from '@/store/auth.store';

interface Props {
    roles: UserSummary['roles'];
    children: React.ReactNode;
}

export function RequireRole({ roles, children }: Props) {
    const { user, isInitializing } = useAuthStore();
    const location = useLocation();

    if (isInitializing) return null;

    if (!user) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    const hasRole = user.roles.some((r) => roles.includes(r));
    if (!hasRole) {
        return <Navigate to="/" replace />;
    }

    return <>{children}</>;
}
