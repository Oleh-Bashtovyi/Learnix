import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { InstructorConversationList } from './components/InstructorConversationList';
import { ConversationView } from '@/components/common/ConversationView';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { cn } from '@/utils/cn';
import type { ConversationSummary } from '@/types/message.types';

export default function InstructorMessagesPage() {
    const { t } = useTranslation('messages');
    const [selected, setSelected] = useState<ConversationSummary | null>(null);

    const { data: conversations = [], isLoading } = useQuery({
        queryKey: queryKeys.messages.conversations(),
        queryFn: messagesApi.getConversations,
    });

    useEffect(() => {
        setSelected((prev) => {
            if (!prev) return prev;
            const fresh = conversations.find((c) => c.id === prev.id);
            return fresh ?? prev;
        });
    }, [conversations]);

    if (isLoading) {
        return (
            <div className="flex h-full items-center justify-center">
                <LoadingSpinner />
            </div>
        );
    }

    return (
        <div className="flex h-full overflow-hidden">
            {/* Conversation list sidebar */}
            <aside
                className={cn(
                    'w-full shrink-0 flex-col overflow-hidden border-r border-border bg-card md:flex md:w-80 lg:w-96',
                    selected ? 'hidden' : 'flex',
                )}
            >
                <div className="shrink-0 border-b border-border px-4 py-3">
                    <h1 className="font-heading text-lg font-semibold text-foreground">
                        {t('instructorPageTitle')}
                    </h1>
                </div>
                <div className="min-h-0 flex-1 overflow-y-auto">
                    <InstructorConversationList
                        conversations={conversations}
                        selectedId={selected?.id ?? null}
                        onSelect={setSelected}
                    />
                </div>
            </aside>

            {/* Chat area */}
            <main
                className={cn(
                    'min-w-0 flex-1 flex-col overflow-hidden bg-background',
                    selected ? 'flex' : 'hidden md:flex',
                )}
            >
                {selected ? (
                    <ConversationView conversation={selected} onBack={() => setSelected(null)} />
                ) : (
                    <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                        {t('selectConversation')}
                    </div>
                )}
            </main>
        </div>
    );
}
