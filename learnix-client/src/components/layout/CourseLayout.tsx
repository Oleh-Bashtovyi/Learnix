import { Outlet } from 'react-router-dom';
import { useNotificationsHub } from '@/hooks/realtime/useNotificationsHub';

export function CourseLayout() {
    useNotificationsHub();
    return <Outlet />;
}
