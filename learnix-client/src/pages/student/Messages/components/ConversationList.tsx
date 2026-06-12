import { useTranslation } from 'react-i18next';
import { formatRelativeTime } from '@/utils/formatDate';
import { cn } from '@/utils/cn';
import type { ConversationSummary } from '@/types/message.types';

interface ConversationListProps {
    conversations: ConversationSummary[];
    selectedId: string | null;
    onSelect: (conversation: ConversationSummary) => void;
}

export function ConversationList({ conversations, selectedId, onSelect }: ConversationListProps) {
    const { t } = useTranslation('messages');

    if (conversations.length === 0) {
        return (
            <div className="p-4 text-center text-sm text-muted-foreground">
                <p>{t('noConversations')}</p>
                <p className="mt-1">{t('noConversationsStudent')}</p>
            </div>
        );
    }

    return (
        <ul className="divide-y divide-border">
            {conversations.map((c) => (
                <li key={c.id}>
                    <button
                        onClick={() => onSelect(c)}
                        className={cn(
                            'w-full px-4 py-3 text-left transition-colors hover:bg-muted/50',
                            selectedId === c.id && 'bg-muted',
                        )}
                    >
                        <div className="flex items-start justify-between gap-2">
                            <div className="min-w-0 flex-1">
                                <p className="truncate font-medium text-foreground">
                                    {c.otherUserName}
                                </p>
                                <p className="truncate text-xs text-muted-foreground">
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
        </ul>
    );
}
