import { MessageSquare } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';

export function MessagesButton() {
    const { t } = useTranslation('header');
    const user = useAuthStore((s) => s.user);

    const { data: messagesData } = useQuery({
        queryKey: queryKeys.messages.unreadCount(),
        queryFn: messagesApi.getUnreadCount,
        enabled: !!user,
        staleTime: Infinity,
    });

    const unread = messagesData?.totalUnread ?? 0;
    const to = user?.role === 'Instructor' ? '/instructor/messages' : '/messages';

    return (
        <Link
            to={to}
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
            aria-label={t('messagesAriaLabel')}
        >
            <MessageSquare className="h-5 w-5" />
            {unread > 0 && (
                <span className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                    {unread > 99 ? '99+' : unread}
                </span>
            )}
        </Link>
    );
}
