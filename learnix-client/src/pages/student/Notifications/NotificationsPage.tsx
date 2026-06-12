import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Trophy, Award, CheckCircle2, XCircle, MessageSquare } from 'lucide-react';
import { cn } from '@/utils/cn';
import { notificationsApi } from '@/api/notifications.api';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { formatRelativeTime } from '@/utils/formatDate';
import type { NotificationDto, NotificationEventType } from '@/types/notification.types';
import type { ConversationSummary } from '@/types/message.types';
import { useAuthStore } from '@/store/auth.store';

const TYPE_ICON: Record<NotificationEventType, React.ReactNode> = {
    AchievementEarned: <Trophy size={16} className="text-warning" />,
    CertificateReady: <Award size={16} className="text-success" />,
    InstructorApproved: <CheckCircle2 size={16} className="text-success" />,
    InstructorRejected: <XCircle size={16} className="text-destructive" />,
};

const TYPE_ROUTE: Record<NotificationEventType, string> = {
    AchievementEarned: '/achievements',
    CertificateReady: '/certificates',
    InstructorApproved: '/become-instructor',
    InstructorRejected: '/become-instructor',
};

function NotificationItem({
    notification,
    onRead,
}: {
    notification: NotificationDto;
    onRead: (id: string) => void;
}) {
    const navigate = useNavigate();

    function handleClick() {
        if (!notification.isRead) onRead(notification.id);
        navigate(TYPE_ROUTE[notification.type]);
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
                    {notification.title}
                </p>
                <p className="mt-0.5 text-sm text-muted-foreground">{notification.body}</p>
                <p className="mt-1 text-xs text-muted-foreground">
                    {formatRelativeTime(notification.createdAt)}
                </p>
            </div>
            {!notification.isRead && (
                <span className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-primary" />
            )}
        </button>
    );
}

function ConversationItem({ conversation }: { conversation: ConversationSummary }) {
    const navigate = useNavigate();
    const user = useAuthStore((s) => s.user);
    const messagesPath = user?.role === 'Instructor' ? '/instructor/messages' : '/messages';

    return (
        <button
            onClick={() => navigate(messagesPath)}
            className="flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/50"
        >
            <div className="mt-0.5 shrink-0">
                <MessageSquare size={16} className="text-muted-foreground" />
            </div>
            <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-foreground">
                    {conversation.otherUserName}
                </p>
                <p className="truncate text-xs text-muted-foreground">{conversation.courseName}</p>
                {conversation.lastMessagePreview && (
                    <p className="mt-0.5 truncate text-sm text-muted-foreground">
                        {conversation.lastMessagePreview}
                    </p>
                )}
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1">
                {conversation.lastMessageAt && (
                    <span className="text-xs text-muted-foreground">
                        {formatRelativeTime(conversation.lastMessageAt)}
                    </span>
                )}
                {conversation.unreadCount > 0 && (
                    <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1 text-xs font-bold text-primary-foreground">
                        {conversation.unreadCount}
                    </span>
                )}
            </div>
        </button>
    );
}

export default function NotificationsPage() {
    const { t } = useTranslation('notifications');
    const queryClient = useQueryClient();

    const { data: notifications = [] } = useQuery({
        queryKey: queryKeys.notifications.list(),
        queryFn: notificationsApi.getAll,
    });

    const { data: conversations = [] } = useQuery({
        queryKey: queryKeys.messages.conversations(),
        queryFn: messagesApi.getConversations,
    });

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
            <div className="mb-6 flex items-center justify-between">
                <h1 className="font-heading text-2xl font-bold text-foreground">{t('title')}</h1>
                {hasUnread && (
                    <button
                        onClick={() => markAllReadMutation.mutate()}
                        disabled={markAllReadMutation.isPending}
                        className="text-sm text-primary hover:underline disabled:opacity-50"
                    >
                        {t('markAllRead')}
                    </button>
                )}
            </div>

            <div className="overflow-hidden rounded-xl border border-border bg-card">
                {/* System notifications */}
                <div className="border-b border-border px-4 py-2.5">
                    <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                        {t('systemSection')}
                    </p>
                </div>
                {notifications.length === 0 ? (
                    <p className="px-4 py-6 text-center text-sm text-muted-foreground">
                        {t('emptySystem')}
                    </p>
                ) : (
                    <div className="divide-y divide-border">
                        {notifications.map((n) => (
                            <NotificationItem
                                key={n.id}
                                notification={n}
                                onRead={(id) => markReadMutation.mutate(id)}
                            />
                        ))}
                    </div>
                )}

                {/* Messages */}
                <div className="border-y border-border px-4 py-2.5">
                    <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                        {t('messagesSection')}
                    </p>
                </div>
                {conversations.length === 0 ? (
                    <p className="px-4 py-6 text-center text-sm text-muted-foreground">
                        {t('emptyMessages')}
                    </p>
                ) : (
                    <div className="max-h-72 divide-y divide-border overflow-y-auto">
                        {conversations.map((c) => (
                            <ConversationItem key={c.id} conversation={c} />
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
