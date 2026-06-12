import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore, type UserSummary } from '@/store/auth.store';

interface Props {
    roles: UserSummary['role'][];
    children: React.ReactNode;
}

export function RequireRole({ roles, children }: Props) {
    const { user, isInitializing } = useAuthStore();
    const location = useLocation();

    if (isInitializing) return null;

    if (!user) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    if (!roles.includes(user.role)) {
        return <Navigate to="/" replace />;
    }

    return <>{children}</>;
}
