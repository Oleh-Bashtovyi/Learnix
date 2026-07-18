export type NotificationEventType =
    | 'AchievementEarned'
    | 'CertificateReady'
    | 'InstructorApproved'
    | 'InstructorRejected'
    | 'RoleAssigned'
    | 'RoleRemoved';

/**
 * Values the translation needs — `{ courseTitle }`, `{ code }` — or absent when the type is the whole
 * message. The server reports what happened; the wording is ours (ADR-NOTIF-001).
 */
export type NotificationParams = Record<string, string>;

export interface NotificationDto {
    id: string;
    type: NotificationEventType;
    parameters: NotificationParams | null;
    isRead: boolean;
    createdAt: string;
}

export interface UnreadNotificationCountDto {
    count: number;
}

export interface NotificationReceivedPayload {
    notificationId: string;
    type: NotificationEventType;
    parameters: NotificationParams | null;
    createdAt: string;
}
