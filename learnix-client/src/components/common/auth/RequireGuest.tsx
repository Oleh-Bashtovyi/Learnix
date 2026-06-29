import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/store/auth.store';
import { getRoleHome } from '@/utils/getRoleHome';

interface RequireGuestProps {
    children: React.ReactElement;
}

export function RequireGuest({ children }: RequireGuestProps) {
    const { user, isInitializing } = useAuthStore();
    const location = useLocation();

    if (isInitializing) return null;

    if (user) {
        // If the user is logged in, redirect them to their home page.
        // We use state={{ from: location }} if we want to remember where they came from,
        // but typically for a guest guard we just send them to their dashboard.
        const homePath = getRoleHome(user.roles);
        return <Navigate to={homePath} replace state={{ from: location }} />;
    }

    return children;
}
