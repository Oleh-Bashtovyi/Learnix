import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/store/auth.store';
import { queryKeys } from '@/api/queryKeys';
import { env } from '@/utils/env';

interface AchievementUnlockedPayload {
    achievementId: string;
    code: string;
    unlockedAt: string;
}

export function useAchievementsHub() {
    const accessToken = useAuthStore((s) => s.accessToken);
    const queryClient = useQueryClient();
    const { t } = useTranslation('achievements');
    const tRef = useRef(t);
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        tRef.current = t;
    });

    useEffect(() => {
        if (!accessToken) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${env.HUB_URL}/hubs/achievements`, {
                accessTokenFactory: () => accessToken,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('AchievementUnlocked', (payload: AchievementUnlockedPayload) => {
            const translate = tRef.current;
            toast.success(`🏆 ${translate(`meta.${payload.code}.name`, payload.code)}`, {
                description: translate(`meta.${payload.code}.description`),
                duration: 6000,
            });

            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });
        });

        connection.start().catch(() => {});

        connectionRef.current = connection;

        return () => {
            connection.stop();
        };
    }, [accessToken, queryClient]);
}
