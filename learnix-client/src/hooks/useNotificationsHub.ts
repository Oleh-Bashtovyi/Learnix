import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/api/queryKeys';
import { useAuthStore } from '@/store/auth.store';
import type { CertificateReadyNotification } from '@/types/certificate.types';
import type { NewMessageNotification, UnreadCountNotification } from '@/types/message.types';
import type { NotificationReceivedPayload } from '@/types/notification.types';
import { env } from '@/utils/env';

interface AchievementUnlockedPayload {
    achievementId: string;
    code: string;
    unlockedAt: string;
}

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
            toast.success(
                `🏆 ${tAchievements(`meta.${payload.code}.name`, { defaultValue: payload.code })}`,
                {
                    description: tAchievements(`meta.${payload.code}.description`),
                    duration: 6000,
                },
            );
            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });
        });

        connection.on('CertificateReady', (payload: CertificateReadyNotification) => {
            toast.success(t('notification.title'), {
                description: `${t('notification.descriptionPrefix')}"${payload.courseTitle}"`,
                duration: 8000,
                action: {
                    label: t('notification.action'),
                    onClick: () => navigateRef.current('/certificates'),
                },
            });
            queryClient.invalidateQueries({ queryKey: queryKeys.certificates.mine() });
        });

        connection.on('NotificationReceived', (_: NotificationReceivedPayload) => {
            queryClient.setQueryData<{ count: number }>(
                queryKeys.notifications.unreadCount(),
                (old) => ({ count: (old?.count ?? 0) + 1 }),
            );
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
        });

        connection.start().catch(() => {});
        connectionRef.current = connection;

        return () => {
            connection.stop();
        };
    }, [accessToken, queryClient]); // eslint-disable-line react-hooks/exhaustive-deps
}
