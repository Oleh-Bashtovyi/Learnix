import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/api/queryKeys';
import { AchievementToast } from '@/components/common/system/AchievementToast';
import { CertificateToast } from '@/components/common/system/CertificateToast';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import type { CertificateIssuedNotification } from '@/types/certificate.types';
import type { NewMessageNotification, UnreadCountNotification } from '@/types/message.types';
import type { NotificationReceivedPayload } from '@/types/notification.types';
import { env } from '@/utils/env';
import { refreshSession } from '@/utils/refreshSession';

interface AchievementUnlockedPayload {
    achievementId: string;
    code: string;
    unlockedAt: string;
}

/**
 * Related ADRs:
 * - ADR-FRONT-API-004: Realtime Communication via a Single SignalR Notifications Hub
 * - ADR-FRONT-AUTH-006: Mid-Session Role Change Forces a Token Refresh
 */
export function useNotificationsHub() {
    const accessToken = useAuthStore((s) => s.accessToken);
    const queryClient = useQueryClient();
    const navigate = useNavigate();
    const navigateRef = useRef(navigate);

    useEffect(() => {
        navigateRef.current = navigate;
    }, [navigate]);

    const connectionRef = useRef<signalR.HubConnection | null>(null);
    const { t } = useTranslation('certificates');
    const { t: tAchievements } = useTranslation('achievements');

    useEffect(() => {
        if (!accessToken) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${env.HUB_URL}/hubs/notifications`, {
                accessTokenFactory: () => accessToken,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', (notification: NewMessageNotification) => {
            queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
            queryClient.invalidateQueries({
                queryKey: queryKeys.messages.messages(notification.conversationId),
            });
        });

        connection.on('UnreadCountChanged', (notification: UnreadCountNotification) => {
            queryClient.setQueryData(queryKeys.messages.unreadCount(), {
                totalUnread: notification.totalUnread,
            });
        });

        connection.on('AchievementUnlocked', (payload: AchievementUnlockedPayload) => {
            toast.custom(
                (id) => (
                    <AchievementToast
                        name={tAchievements(`meta.${payload.code}.name`, {
                            defaultValue: payload.code,
                        })}
                        description={tAchievements(`meta.${payload.code}.description`, {
                            defaultValue: '',
                        })}
                        onViewAll={() => {
                            toast.dismiss(id);
                            navigateRef.current(APP_ROUTES.student.achievements);
                        }}
                        onDismiss={() => toast.dismiss(id)}
                    />
                ),
                { duration: 8000 },
            );
            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });
        });

        connection.on('CertificateIssued', (payload: CertificateIssuedNotification) => {
            toast.custom(
                (id) => (
                    <CertificateToast
                        courseTitle={payload.courseTitle}
                        onViewAll={() => {
                            toast.dismiss(id);
                            navigateRef.current(APP_ROUTES.student.certificates);
                        }}
                        onRate={() => {
                            toast.dismiss(id);
                            navigateRef.current(
                                `${APP_ROUTES.public.courseDetail(payload.courseId)}#reviews`,
                            );
                        }}
                        onDismiss={() => toast.dismiss(id)}
                    />
                ),
                { duration: 8000 },
            );
            queryClient.invalidateQueries({ queryKey: queryKeys.certificates.mine() });
        });

        connection.on('NotificationReceived', (payload: NotificationReceivedPayload) => {
            queryClient.setQueryData<{ count: number }>(
                queryKeys.notifications.unreadCount(),
                (old) => ({ count: (old?.count ?? 0) + 1 }),
            );
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });

            // A role change doesn't revoke the JWT, so the store still holds the old roles until the
            // token is swapped. Refresh now so the granted role's dashboard becomes reachable — and a
            // revoked one stops being — the instant the bell arrives, not up to 15 minutes later
            // (ADR-BACK-NOTIF-002).
            if (payload.type === 'RoleAssigned' || payload.type === 'RoleRemoved') {
                refreshSession().catch(() => {});
            }
        });

        connection.start().catch(() => {});
        connectionRef.current = connection;

        return () => {
            connection.stop();
        };
    }, [accessToken, queryClient, t, tAchievements]);
}
