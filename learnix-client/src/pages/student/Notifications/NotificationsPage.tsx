import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Award, BellOff, CheckCircle2, Trophy, XCircle } from 'lucide-react';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';
import { QueryError } from '@/components/common/system/QueryError';
import { APP_ROUTES } from '@/routes/paths';
import type {
    NotificationDto,
    NotificationEventType,
    NotificationParams,
} from '@/types/notification.types';
import { cn } from '@/utils/cn';
import { formatRelativeTime } from '@/utils/formatDate';

const TYPE_ICON: Record<NotificationEventType, React.ReactNode> = {
    AchievementEarned: <Trophy size={16} className="text-warning" />,
    CertificateReady: <Award size={16} className="text-success" />,
    InstructorApproved: <CheckCircle2 size={16} className="text-success" />,
    InstructorRejected: <XCircle size={16} className="text-destructive" />,
};

const TYPE_ROUTE: Record<NotificationEventType, string> = {
    AchievementEarned: APP_ROUTES.student.achievements,
    CertificateReady: APP_ROUTES.student.certificates,
    InstructorApproved: APP_ROUTES.public.becomeInstructor,
    InstructorRejected: APP_ROUTES.public.becomeInstructor,
};

type NotificationItemProps = {
    notification: NotificationDto;
    onRead: (id: string) => void;
};

function NotificationItem({ notification, onRead }: NotificationItemProps) {
    const navigate = useNavigate();
    const { t } = useTranslation('notifications');
    const { t: tAchievements } = useTranslation('achievements');

    function handleClick() {
        if (!notification.isRead) onRead(notification.id);
        navigate(TYPE_ROUTE[notification.type]);
    }

    // The server sends the type and the facts; the words are ours (ADR-NOTIF-001). An achievement arrives as
    // its code, which the achievements namespace already knows a name for — the server never sent one.
    const params: NotificationParams = { ...notification.parameters };

    if (params.code) {
        params.achievement = tAchievements(`meta.${params.code}.name`, {
            defaultValue: params.code,
        });
    }

    return (
        <button
            onClick={handleClick}
            className={cn(
                'flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/50',
                !notification.isRead && 'bg-primary/5',
            )}
        >
            <div className="mt-0.5 shrink-0">{TYPE_ICON[notification.type]}</div>
            <div className="min-w-0 flex-1">
                <p className={cn('text-sm text-foreground', !notification.isRead && 'font-medium')}>
                    {t(`items.${notification.type}.title`)}
                </p>
                <p className="mt-0.5 text-sm text-muted-foreground">
                    {t(`items.${notification.type}.body`, params)}
                </p>
                <p className="mt-1 text-xs text-muted-foreground">
                    {formatRelativeTime(notification.createdAt)}
                </p>
            </div>
            {!notification.isRead && (
                <span className="mt-1.5 size-2 shrink-0 rounded-full bg-primary" />
            )}
        </button>
    );
}

export default function NotificationsPage() {
    const { t } = useTranslation('notifications');
    const queryClient = useQueryClient();

    const {
        data: notifications = [],
        isError: isNotificationsError,
        refetch: refetchNotifications,
    } = useQuery({
        queryKey: queryKeys.notifications.list(),
        queryFn: notificationsApi.getAll,
    });

    const isError = isNotificationsError;

    const markReadMutation = useMutation({
        mutationFn: notificationsApi.markRead,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.unreadCount() });
        },
    });

    const markAllReadMutation = useMutation({
        mutationFn: notificationsApi.markAllRead,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
            queryClient.setQueryData(queryKeys.notifications.unreadCount(), { count: 0 });
        },
    });

    const hasUnread = notifications.some((n) => !n.isRead);

    return (
        <div className="mx-auto max-w-2xl px-4 py-8">
            {/* Wraps rather than squeezes: the label is one long phrase in Ukrainian, and on a narrow
                screen a gapless justify-between pushes it flush against the heading. */}
            <div className="mb-6 flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
                <h1 className="font-heading text-2xl font-bold text-foreground">
                    {t('common:navigation.notifications')}
                </h1>
                {hasUnread && (
                    <button
                        onClick={() => markAllReadMutation.mutate()}
                        disabled={markAllReadMutation.isPending}
                        className="shrink-0 text-sm text-primary hover:underline disabled:opacity-50"
                    >
                        {t('markAllRead')}
                    </button>
                )}
            </div>

            {/* Notifications only. Conversations used to sit under them, which made this the third place
                the same threads were listed — after the Messages page and the chat icon in the header,
                both of which carry their own unread badge. A conversation is not an event: it goes on,
                where a notification happens once and is read. Mixing them even broke "mark all read",
                which only ever marked the notifications and left the message badge standing.

                It is also what made the page unbounded: the notifications themselves are capped server-side
                at NotificationConstants.MaxPerUser, so on their own they are a list, not a scroll. */}
            {isError ? (
                <QueryError
                    message={t('error.title')}
                    onRetry={refetchNotifications}
                    retryLabel={t('common:actions.tryAgain')}
                    className="rounded-xl border border-border bg-card"
                />
            ) : notifications.length === 0 ? (
                <div className="rounded-xl border border-border bg-card px-4 py-12 text-center">
                    <div className="mx-auto grid size-12 place-items-center rounded-full bg-muted text-muted-foreground">
                        <BellOff className="size-6" />
                    </div>
                    <p className="mt-4 text-sm text-muted-foreground">{t('emptySystem')}</p>
                </div>
            ) : (
                <div className="divide-y divide-border overflow-hidden rounded-xl border border-border bg-card">
                    {notifications.map((n) => (
                        <NotificationItem
                            key={n.id}
                            notification={n}
                            onRead={(id) => markReadMutation.mutate(id)}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}
