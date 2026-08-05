import { useQuery } from '@tanstack/react-query';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';
import { useAuthStore } from '@/store/auth.store';

export function useNotificationsCount() {
    const user = useAuthStore((s) => s.user);

    return useQuery({
        queryKey: queryKeys.notifications.unreadCount(),
        queryFn: notificationsApi.getUnreadCount,
        enabled: !!user,
        staleTime: Infinity,
    });
}
