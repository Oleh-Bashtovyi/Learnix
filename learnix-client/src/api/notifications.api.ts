import type { NotificationDto, UnreadNotificationCountDto } from '@/types/notification.types';
import { api } from './axios.instance';

export const notificationsApi = {
    getAll: () => api.get<NotificationDto[]>('/notifications').then((r) => r.data),

    getUnreadCount: () =>
        api.get<UnreadNotificationCountDto>('/notifications/unread-count').then((r) => r.data),

    markRead: (notificationId: string) =>
        api.post(`/notifications/${notificationId}/read`).then((r) => r.data),

    markAllRead: () => api.post('/notifications/read-all').then((r) => r.data),

    markReadByType: (type: import('@/types/notification.types').NotificationEventType) =>
        api.post(`/notifications/read-by-type?type=${type}`).then((r) => r.data),
};
