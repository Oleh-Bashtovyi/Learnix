import { MessageSquare } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';

export function NotificationBell() {
    const user = useAuthStore((s) => s.user);

    const { data } = useQuery({
        queryKey: queryKeys.messages.unreadCount(),
        queryFn: messagesApi.getUnreadCount,
        enabled: !!user,
        staleTime: Infinity,
    });

    const count = data?.totalUnread ?? 0;
    const messagesPath = user?.role === 'Instructor' ? '/instructor/messages' : '/messages';

    return (
        <Link
            to={messagesPath}
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
            aria-label={count > 0 ? `${count} unread messages` : 'Messages'}
        >
            <MessageSquare className="h-5 w-5" />
            {count > 0 && (
                <span className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                    {count > 99 ? '99+' : count}
                </span>
            )}
        </Link>
    );
}
