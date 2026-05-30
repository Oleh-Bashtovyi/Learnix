import { useState, useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { ConversationList } from './components/ConversationList';
import { ConversationView } from '@/components/common/ConversationView';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { MESSAGES } from '@/const/localization/messages';
import type { ConversationDetail, ConversationSummary } from '@/types/message.types';

export default function MessagesPage() {
    const location = useLocation();
    const initialConversation =
        (location.state as { initialConversation?: ConversationDetail } | null)
            ?.initialConversation ?? null;

    const [selected, setSelected] = useState<ConversationSummary | null>(() => {
        if (!initialConversation) return null;
        return { ...initialConversation, lastMessagePreview: null, lastMessageAt: null };
    });

    const { data: conversations = [], isLoading } = useQuery({
        queryKey: queryKeys.messages.conversations(),
        queryFn: messagesApi.getConversations,
    });

    const initialSyncDone = useRef(false);

    useEffect(() => {
        if (conversations.length === 0) return;

        if (initialConversation && !initialSyncDone.current) {
            // First load only: select the conversation we came from
            const match = conversations.find((c) => c.id === initialConversation.id);
            if (match) {
                setSelected(match);
                initialSyncDone.current = true;
            }
            return;
        }

        // On subsequent refetches: refresh the currently selected conversation's data
        setSelected((prev) => {
            if (!prev) return prev;
            const fresh = conversations.find((c) => c.id === prev.id);
            return fresh ?? prev;
        });
    }, [conversations]); // eslint-disable-line react-hooks/exhaustive-deps

    if (isLoading && !selected) {
        return (
            <div className="flex h-full items-center justify-center">
                <LoadingSpinner />
            </div>
        );
    }

    return (
        <div className="flex h-full overflow-hidden">
            {/* Conversation list sidebar */}
            <aside className="flex w-72 shrink-0 flex-col overflow-hidden border-r border-border bg-card lg:w-80">
                <div className="shrink-0 border-b border-border px-4 py-3">
                    <h1 className="font-heading text-lg font-semibold text-foreground">
                        {MESSAGES.PAGE_TITLE}
                    </h1>
                </div>
                <div className="min-h-0 flex-1 overflow-y-auto">
                    <ConversationList
                        conversations={conversations}
                        selectedId={selected?.id ?? null}
                        onSelect={setSelected}
                    />
                </div>
            </aside>

            {/* Chat area */}
            <main className="flex min-w-0 flex-1 flex-col overflow-hidden bg-background">
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
