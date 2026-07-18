import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
    Award,
    BellOff,
    CheckCircle2,
    ShieldCheck,
    ShieldOff,
    Trophy,
    XCircle,
} from 'lucide-react';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';
import { TextButton } from '@/components/common/elements/TextButton';
import { QueryError } from '@/components/common/system/QueryError';
import { NOTIFICATION_ICON_SIZE } from '@/const/ui.constants';
import { UserRole } from '@/enums/user.enums';
import { APP_ROUTES } from '@/routes/paths';
import type {
    NotificationDto,
    NotificationEventType,
    NotificationParams,
} from '@/types/notification.types';
import { cn } from '@/utils/cn';
import { formatRelativeTime } from '@/utils/formatDate';

const TYPE_ICON: Record<NotificationEventType, React.ReactNode> = {
    AchievementEarned: <Trophy size={NOTIFICATION_ICON_SIZE.typeBadge} className="text-warning" />,
    CertificateReady: <Award size={NOTIFICATION_ICON_SIZE.typeBadge} className="text-success" />,
    InstructorApproved: (
        <CheckCircle2 size={NOTIFICATION_ICON_SIZE.typeBadge} className="text-success" />
    ),
    InstructorRejected: (
        <XCircle size={NOTIFICATION_ICON_SIZE.typeBadge} className="text-destructive" />
    ),
    RoleAssigned: <ShieldCheck size={NOTIFICATION_ICON_SIZE.typeBadge} className="text-success" />,
    RoleRemoved: <ShieldOff size={NOTIFICATION_ICON_SIZE.typeBadge} className="text-destructive" />,
};

const TYPE_ROUTE: Record<NotificationEventType, string> = {
    AchievementEarned: APP_ROUTES.student.achievements,
    CertificateReady: APP_ROUTES.student.certificates,
    InstructorApproved: APP_ROUTES.public.becomeInstructor,
    InstructorRejected: APP_ROUTES.public.becomeInstructor,
    // A revoked role, or a grant of a role with no dashboard (Admin's is guarded separately), lands on
    // the profile. RoleAssigned to a role that unlocks a dashboard overrides this — see resolveRoute.
    RoleAssigned: APP_ROUTES.student.profile,
    RoleRemoved: APP_ROUTES.student.profile,
};

/**
 * A `RoleAssigned` deep-links to the dashboard the role just unlocked; the target depends on which role
 * (carried in `{ role }`), so it can't live in the static by-type map. The token was already refreshed
 * when the notification arrived (ADR-BACK-NOTIF-002), so the route guard sees the new role by the time
 * this is clicked.
 */
function resolveRoute(notification: NotificationDto): string {
    if (notification.type === 'RoleAssigned') {
        if (notification.parameters?.role === UserRole.Instructor)
            return APP_ROUTES.instructor.dashboard;
        if (notification.parameters?.role === UserRole.Admin) return APP_ROUTES.admin.dashboard;
    }
    return TYPE_ROUTE[notification.type];
}

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
        navigate(resolveRoute(notification));
    }

    // The server sends the type and the facts; the words are ours (ADR-NOTIF-001). An achievement arrives as
    // its code, which the achievements namespace already knows a name for — the server never sent one.
    const params: NotificationParams = { ...notification.parameters };

    if (params.code) {
        params.achievement = tAchievements(`meta.${params.code}.name`, {
            defaultValue: params.code,
        });
    }

    // A role change carries the raw role name ("Instructor"/"Admin"); localize it the same way.
    if (params.role) {
        params.role = t(`common:roles.${params.role.toLowerCase()}`, {
            defaultValue: params.role,
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
                    <TextButton
                        onClick={() => markAllReadMutation.mutate()}
                        disabled={markAllReadMutation.isPending}
                        className="shrink-0"
                    >
                        {t('markAllRead')}
                    </TextButton>
                )}
            </div>

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
                        <BellOff size={NOTIFICATION_ICON_SIZE.emptyState} />
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
