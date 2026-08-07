import { useTranslation } from 'react-i18next';
import { MessagesSquare } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import type { ConversationSummary } from '@/types/message.types';
import { cn } from '@/utils/cn';
import { formatRelativeTime } from '@/utils/formatDate';

interface ConversationListProps {
    conversations: ConversationSummary[];
    selectedId: string | null;
    onSelect: (conversation: ConversationSummary) => void;
    isFetchingNextPage?: boolean;
    variant?: 'student' | 'instructor' | 'admin';
}

export function ConversationList({
    conversations,
    selectedId,
    onSelect,
    isFetchingNextPage,
    variant = 'student',
}: ConversationListProps) {
    const { t } = useTranslation('messages');

    if (conversations.length === 0) {
        return (
            <div className="p-8 text-center">
                <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-accent/10">
                    <MessagesSquare className="size-7 text-accent-strong" aria-hidden="true" />
                </div>
                <p className="mt-4 font-medium text-foreground">{t('noConversations')}</p>
                {variant === 'student' && (
                    <p className="mt-1 text-sm text-muted-foreground">
                        {t('noConversationsStudent')}
                    </p>
                )}
            </div>
        );
    }

    return (
        <ul className="divide-y divide-border">
            {conversations.map((c) => (
                <li key={c.id}>
                    <button
                        type="button"
                        onClick={() => onSelect(c)}
                        className={cn(
                            'w-full border-l-2 border-transparent px-4 py-3 text-left transition-colors hover:bg-muted/50',
                            selectedId === c.id && 'border-primary bg-primary/10',
                        )}
                    >
                        <div className="flex items-start justify-between gap-2">
                            <div className="min-w-0 flex-1">
                                <div className="flex items-center gap-1.5">
                                    <p className="truncate font-medium text-foreground">
                                        {c.otherUserName}
                                    </p>
                                    {c.isBlocked && (
                                        <Badge variant="destructive" className="shrink-0">
                                            {t('blocked')}
                                        </Badge>
                                    )}
                                </div>
                                <p
                                    className={cn(
                                        'truncate text-xs',
                                        variant === 'instructor'
                                            ? 'font-medium text-primary'
                                            : 'text-muted-foreground',
                                    )}
                                >
                                    {c.courseName}
                                </p>
                                {c.lastMessagePreview && (
                                    <p className="mt-0.5 truncate text-sm text-muted-foreground">
                                        {c.lastMessagePreview}
                                    </p>
                                )}
                            </div>
                            <div className="flex shrink-0 flex-col items-end gap-1">
                                {c.lastMessageAt && (
                                    <span className="text-xs text-muted-foreground">
                                        {formatRelativeTime(c.lastMessageAt)}
                                    </span>
                                )}
                                {c.unreadCount > 0 && (
                                    <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1 text-xs font-bold text-primary-foreground">
                                        {c.unreadCount}
                                    </span>
                                )}
                            </div>
                        </div>
                    </button>
                </li>
            ))}
            {isFetchingNextPage && (
                <li className="flex justify-center p-4">
                    <span className="text-xs text-muted-foreground">
                        {t('loadingMore', 'Loading more...')}
                    </span>
                </li>
            )}
        </ul>
    );
}
