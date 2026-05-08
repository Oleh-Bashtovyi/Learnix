import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { useChatHub } from '@/hooks/useChatHub';
import { InstructorConversationList } from './components/InstructorConversationList';
import { ConversationView } from '@/components/common/ConversationView';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { MESSAGES } from '@/const/localization/messages';
import type { ConversationSummary } from '@/types/message.types';

export default function InstructorMessagesPage() {
    useChatHub();
    const [selected, setSelected] = useState<ConversationSummary | null>(null);

    const { data: conversations = [], isLoading } = useQuery({
        queryKey: queryKeys.messages.conversations(),
        queryFn: messagesApi.getConversations,
    });

    if (isLoading) {
        return (
            <div className="flex h-full items-center justify-center">
                <LoadingSpinner />
            </div>
        );
    }

    const totalUnread = conversations.reduce((sum, c) => sum + c.unreadCount, 0);

    return (
        <div className="flex h-[calc(100vh-4rem)] overflow-hidden">
            <aside className="w-80 shrink-0 overflow-y-auto border-r border-border bg-card">
                <div className="border-b border-border px-4 py-3">
                    <div className="flex items-center justify-between">
                        <h1 className="font-heading text-lg font-semibold text-foreground">
                            {MESSAGES.INSTRUCTOR_PAGE_TITLE}
                        </h1>
                        {totalUnread > 0 && (
                            <span className="flex h-6 min-w-6 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-bold text-primary-foreground">
                                {totalUnread}
                            </span>
                        )}
                    </div>
                </div>
                <InstructorConversationList
                    conversations={conversations}
                    selectedId={selected?.id ?? null}
                    onSelect={setSelected}
                />
            </aside>

            <main className="flex-1 overflow-hidden bg-background">
                {selected ? (
                    <ConversationView conversation={selected} />
                ) : (
                    <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                        {MESSAGES.SELECT_CONVERSATION}
                    </div>
                )}
            </main>
        </div>
    );
}
