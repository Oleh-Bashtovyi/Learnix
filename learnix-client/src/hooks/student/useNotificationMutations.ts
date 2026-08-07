import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';

export function useMarkNotificationRead() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (notificationId: string) => notificationsApi.markRead(notificationId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.unreadCount() });
        },
    });
}

export function useMarkAllNotificationsRead() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: () => notificationsApi.markAllRead(),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
            queryClient.setQueryData(queryKeys.notifications.unreadCount(), { count: 0 });
        },
    });
}

/** Fired once on mount by AchievementsPage — a background courtesy, not a user action, so a
 *  failure stays silent rather than surfacing the global error toast. */
export function useMarkAchievementNotificationsRead() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: () => notificationsApi.markReadByType('AchievementEarned'),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.unreadCount() });
        },
        meta: { suppressGlobalError: true },
    });
}
