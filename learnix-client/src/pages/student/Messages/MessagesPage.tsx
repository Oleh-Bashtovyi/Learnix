import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { useChatHub } from '@/hooks/useChatHub';
import { ConversationList } from './components/ConversationList';
import { ConversationView } from '@/components/common/ConversationView';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { MESSAGES } from '@/const/localization/messages';
import type { ConversationSummary } from '@/types/message.types';

export default function MessagesPage() {
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

    return (
        <div className="flex h-[calc(100vh-4rem)] overflow-hidden">
            <aside className="w-80 shrink-0 overflow-y-auto border-r border-border bg-card">
                <div className="border-b border-border px-4 py-3">
                    <h1 className="font-heading text-lg font-semibold text-foreground">
                        {MESSAGES.PAGE_TITLE}
                    </h1>
                </div>
                <ConversationList
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
