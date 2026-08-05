import { useQuery } from '@tanstack/react-query';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';

export function useNotifications() {
    return useQuery({
        queryKey: queryKeys.notifications.list(),
        queryFn: notificationsApi.getAll,
    });
}
