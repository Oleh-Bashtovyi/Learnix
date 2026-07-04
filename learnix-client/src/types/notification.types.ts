export type NotificationEventType =
    | 'AchievementEarned'
    | 'CertificateReady'
    | 'InstructorApproved'
    | 'InstructorRejected';

export interface NotificationDto {
    id: string;
    type: NotificationEventType;
    title: string;
    body: string;
    isRead: boolean;
    createdAt: string;
}

export interface UnreadNotificationCountDto {
    count: number;
}

export interface NotificationReceivedPayload {
    notificationId: string;
    type: NotificationEventType;
    title: string;
    body: string;
    createdAt: string;
}
