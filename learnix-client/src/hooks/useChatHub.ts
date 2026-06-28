import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { useAuthStore } from '@/store/auth.store';
import type { NewMessageNotification, UnreadCountNotification } from '@/types/message.types';
import { env } from '@/utils/env';

export function useChatHub() {
    const accessToken = useAuthStore((s) => s.accessToken);
    const queryClient = useQueryClient();
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        if (!accessToken) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${env.HUB_URL}/hubs/chat`, {
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

        connection.start().catch(() => {});

        connectionRef.current = connection;

        return () => {
            connection.stop();
        };
    }, [accessToken, queryClient]);
}
