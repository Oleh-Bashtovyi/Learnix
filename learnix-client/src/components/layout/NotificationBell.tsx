import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Bell } from 'lucide-react';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';

export function NotificationBell() {
    const { t } = useTranslation('header');
    const user = useAuthStore((s) => s.user);

    const { data: notifData } = useQuery({
        queryKey: queryKeys.notifications.unreadCount(),
        queryFn: notificationsApi.getUnreadCount,
        enabled: !!user,
        staleTime: Infinity,
    });

    const unread = notifData?.count ?? 0;

    return (
        <Link
            to={APP_ROUTES.student.notifications}
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
            aria-label={t('common:navigation.notifications')}
        >
            <Bell className="size-5" />
            {unread > 0 && (
                <span className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                    {unread > 99 ? '99+' : unread}
                </span>
            )}
        </Link>
    );
}
