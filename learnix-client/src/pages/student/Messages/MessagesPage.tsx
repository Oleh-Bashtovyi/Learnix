import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import {
    keepPreviousData,
    useInfiniteQuery,
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { SharedConversationList } from '@/components/common/messages/SharedConversationList';
import { ConversationView } from '@/components/common/messaging/ConversationView';
import { LoadingSpinner } from '@/components/common/ui/LoadingSpinner';
import { SearchInput } from '@/components/common/ui/SearchInput';
import { TextButton } from '@/components/common/ui/TextButton';
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from '@/components/ui/resizable';
import { useDebounce } from '@/hooks/shared/useDebounce';
import type { ConversationDetail } from '@/types/message.types';
import { NewMessageModal } from './components/NewMessageModal';

interface MessagesPageProps {
    displayTitle?: boolean;
}

export default function MessagesPage({ displayTitle = true }: MessagesPageProps) {
    const { t } = useTranslation('messages');
    const location = useLocation();
    const initialConversation =
        (location.state as { initialConversation?: ConversationDetail } | null)
            ?.initialConversation ?? null;

    const [selectedId, setSelectedId] = useState<string | null>(initialConversation?.id ?? null);

    const [searchQuery, setSearchQuery] = useState('');
    const debouncedSearch = useDebounce(searchQuery, 500);
    const [showNewMessageModal, setShowNewMessageModal] = useState(false);
    const queryClient = useQueryClient();

    const isInstructor = location.pathname.startsWith('/instructor');
    const isAdmin = location.pathname.startsWith('/admin');
    const variant = isAdmin ? 'admin' : isInstructor ? 'instructor' : 'student';

    const startOrGetMutation = useMutation({
        mutationFn: (courseId: string) => messagesApi.startOrGet({ courseId }),
        onSuccess: (conversation) => {
            setShowNewMessageModal(false);
            setSelectedId(conversation.id);
            queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
        },
    });

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

        // Fallback to the initial conversation if it matches selectedId but has not loaded yet.
        // start-or-get only ever opens a student's thread with the course's instructor, so the other
        // party is one by construction — there is no case here where it could be another student.
        if (selectedId === initialConversation?.id) {
            return {
                ...initialConversation,
                lastMessagePreview: null,
                lastMessageAt: null,
                otherUserIsInstructor: true,
            };
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

    const sidebarContent = (
        <>
            <div className="shrink-0 border-b border-border px-4 py-3">
                {displayTitle && (
                    <div className="flex items-center justify-between">
                        <h1 className="font-heading text-lg font-semibold text-foreground">
                            {t('common:navigation.messages')}
                        </h1>
                        {variant === 'student' && (
                            <TextButton onClick={() => setShowNewMessageModal(true)}>
                                {t('newMessage')}
                            </TextButton>
                        )}
                    </div>
                )}
                <div className={displayTitle ? 'pt-3' : ''}>
                    <SearchInput
                        placeholder={t('searchPlaceholder', 'Search...')}
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        onClear={() => setSearchQuery('')}
                        variant="card"
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
        </>
    );

    return (
        <>
            {/* MOBILE LAYOUT (Full width toggle) */}
            <div className="flex size-full overflow-hidden md:hidden">
                {!selected ? (
                    <div className="flex size-full flex-col overflow-hidden bg-card">
                        {sidebarContent}
                    </div>
                ) : (
                    <div className="flex size-full flex-col overflow-hidden bg-background">
                        <ConversationView
                            conversation={selected}
                            onBack={() => setSelectedId(null)}
                        />
                    </div>
                )}
            </div>

            {/* DESKTOP LAYOUT (Resizable) */}
            <div className="hidden size-full overflow-hidden md:flex">
                {/* 
                  CRITICAL WARNING: 
                  ALWAYS use STRINGS for defaultSize, minSize, and maxSize (e.g. "20"). 
                  DO NOT use numbers (e.g. 20). 
                  In this specific wrapper, STRINGS = percentages, NUMBERS = pixels.
                  Using numbers will cause the panels to become microscopic!
                */}
                <ResizablePanelGroup orientation="horizontal" className="size-full overflow-hidden">
                    <ResizablePanel
                        defaultSize="20"
                        minSize="15"
                        maxSize="35"
                        className="flex min-w-0 flex-col overflow-hidden bg-card"
                    >
                        {sidebarContent}
                    </ResizablePanel>

                    <ResizableHandle withHandle />

                    <ResizablePanel
                        defaultSize="80"
                        className="flex min-w-0 flex-col overflow-hidden bg-background"
                    >
                        {selected ? (
                            <ConversationView
                                conversation={selected}
                                onBack={() => setSelectedId(null)}
                            />
                        ) : (
                            <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
                                {t('selectConversation')}
                            </div>
                        )}
                    </ResizablePanel>
                </ResizablePanelGroup>
            </div>

            {showNewMessageModal && (
                <NewMessageModal
                    onClose={() => setShowNewMessageModal(false)}
                    onSelectCourse={(courseId) => startOrGetMutation.mutate(courseId)}
                    isStarting={startOrGetMutation.isPending}
                />
            )}
        </>
    );
}
