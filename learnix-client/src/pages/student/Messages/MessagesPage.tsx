import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import { keepPreviousData, useInfiniteQuery } from '@tanstack/react-query';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { SharedConversationList } from '@/components/common/messages/SharedConversationList';
import { ConversationView } from '@/components/common/messaging/ConversationView';
import { LoadingSpinner } from '@/components/common/ui/LoadingSpinner';
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from '@/components/ui/resizable';
import { useDebounce } from '@/hooks/shared/useDebounce';
import type { ConversationDetail } from '@/types/message.types';
import { cn } from '@/utils/cn';

export default function MessagesPage() {
    const { t } = useTranslation('messages');
    const location = useLocation();
    const initialConversation =
        (location.state as { initialConversation?: ConversationDetail } | null)
            ?.initialConversation ?? null;

    const [selectedId, setSelectedId] = useState<string | null>(initialConversation?.id ?? null);

    const [searchQuery, setSearchQuery] = useState('');
    const debouncedSearch = useDebounce(searchQuery, 500);

    const isInstructor = location.pathname.startsWith('/instructor');
    const isAdmin = location.pathname.startsWith('/admin');
    const variant = isAdmin ? 'admin' : isInstructor ? 'instructor' : 'student';

    /**
     * Related ADRs:
     * - ADR-FRONT-API-008: Pagination Strategies (Infinite Scrolling)
     */
    const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } = useInfiniteQuery({
        queryKey: [...queryKeys.messages.conversations(), debouncedSearch],
        queryFn: ({ pageParam = 0 }) =>
            messagesApi.getConversations(pageParam, 20, debouncedSearch || undefined),
        initialPageParam: 0,
        getNextPageParam: (lastPage) =>
            lastPage.hasNextPage ? lastPage.page * 20 + 20 : undefined,
        placeholderData: keepPreviousData,
    });

    const conversations = useMemo(() => data?.pages.flatMap((p) => p.items) ?? [], [data?.pages]);

    const selected = useMemo(() => {
        if (!selectedId) return null;
        const match = conversations.find((c) => c.id === selectedId);

        if (match) return match;

        // Fallback to initial conversation if it matches selectedId but isn't loaded yet
        if (selectedId === initialConversation?.id) {
            return { ...initialConversation, lastMessagePreview: null, lastMessageAt: null };
        }
        return null;
    }, [conversations, selectedId, initialConversation]);

    if (isLoading && !selected) {
        return (
            <div className="flex h-full items-center justify-center">
                <LoadingSpinner />
            </div>
        );
    }

    return (
        <ResizablePanelGroup orientation="horizontal" className="h-full overflow-hidden">
            {/* Conversation list sidebar */}
            <ResizablePanel
                defaultSize="20"
                minSize="15"
                maxSize="35"
                className={cn(
                    'flex min-w-0 flex-col overflow-hidden bg-card',
                    selected ? 'hidden md:flex' : 'flex',
                )}
            >
                <div className="shrink-0 border-b border-border px-4 py-3">
                    <h1 className="font-heading text-lg font-semibold text-foreground">
                        {t('pageTitle')}
                    </h1>
                    <div className="pt-3">
                        <input
                            type="text"
                            placeholder={t('searchPlaceholder', 'Search...')}
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                        />
                    </div>
                </div>
                <div
                    className="min-h-0 flex-1 overflow-y-auto"
                    onScroll={(e) => {
                        const target = e.target as HTMLDivElement;
                        if (target.scrollHeight - target.scrollTop <= target.clientHeight + 100) {
                            if (hasNextPage && !isFetchingNextPage) {
                                fetchNextPage();
                            }
                        }
                    }}
                >
                    <SharedConversationList
                        conversations={conversations}
                        selectedId={selectedId}
                        onSelect={(conv) => setSelectedId(conv.id)}
                        isFetchingNextPage={isFetchingNextPage}
                        variant={variant}
                    />
                </div>
            </ResizablePanel>

            <ResizableHandle withHandle className="hidden md:flex" />

            {/* Chat area */}
            <ResizablePanel
                defaultSize="80"
                className={cn(
                    'flex min-w-0 flex-col overflow-hidden bg-background',
                    selected ? 'flex' : 'hidden md:flex',
                )}
            >
                {selected ? (
                    <ConversationView conversation={selected} onBack={() => setSelectedId(null)} />
                ) : (
                    <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                        {t('selectConversation')}
                    </div>
                )}
            </ResizablePanel>
        </ResizablePanelGroup>
    );
}
