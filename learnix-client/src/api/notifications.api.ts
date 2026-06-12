import { api } from './axios.instance';
import type { NotificationDto, UnreadNotificationCountDto } from '@/types/notification.types';

export const notificationsApi = {
    getAll: () => api.get<NotificationDto[]>('/notifications').then((r) => r.data),

    getUnreadCount: () =>
        api.get<UnreadNotificationCountDto>('/notifications/unread-count').then((r) => r.data),

    markRead: (notificationId: string) =>
        api.post(`/notifications/${notificationId}/read`).then((r) => r.data),

    markAllRead: () => api.post('/notifications/read-all').then((r) => r.data),
};
