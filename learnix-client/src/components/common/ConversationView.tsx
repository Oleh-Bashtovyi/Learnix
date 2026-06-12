import { useEffect, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { ChevronLeft } from 'lucide-react';
import { ChatMessage } from '@/components/common/ChatMessage';
import { MessageInput } from '@/components/common/MessageInput';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import type { ConversationSummary } from '@/types/message.types';

interface ConversationViewProps {
    conversation: ConversationSummary;
    onBack?: () => void;
}

export function ConversationView({ conversation, onBack }: ConversationViewProps) {
    const { t } = useTranslation('messages');
    const queryClient = useQueryClient();
    const bottomRef = useRef<HTMLDivElement>(null);

    const { data, isLoading } = useQuery({
        queryKey: queryKeys.messages.messages(conversation.id),
        queryFn: () => messagesApi.getMessages(conversation.id),
    });

    const sendMutation = useMutation({
        mutationFn: (content: string) => messagesApi.sendMessage(conversation.id, { content }),
        onSuccess: () => {
            queryClient.invalidateQueries({
                queryKey: queryKeys.messages.messages(conversation.id),
            });
            queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
        },
    });

    useEffect(() => {
        if (conversation.unreadCount > 0) {
            messagesApi.markRead(conversation.id).then(() => {
                queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
                queryClient.invalidateQueries({ queryKey: queryKeys.messages.unreadCount() });
            });
        }
    }, [conversation.id, conversation.unreadCount, queryClient]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [data]);

    const messages = data ? [...(data.items ?? [])].reverse() : [];

    const formatDateDivider = (dateString: string) => {
        const date = new Date(dateString);
        const today = new Date();
        const yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);

        if (date.toDateString() === today.toDateString()) {
            return t('today');
        }
        if (date.toDateString() === yesterday.toDateString()) {
            return t('yesterday');
        }
        return date.toLocaleDateString(undefined, {
            month: 'short',
            day: 'numeric',
            year: date.getFullYear() !== today.getFullYear() ? 'numeric' : undefined,
        });
    };

    return (
        <div className="flex h-full flex-col">
            <div className="flex items-center gap-3 border-b border-border px-4 py-3">
                {onBack && (
                    <button
                        type="button"
                        onClick={onBack}
                        className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted md:hidden"
                        aria-label="Back to conversations"
                    >
                        <ChevronLeft className="h-5 w-5" />
                    </button>
                )}
                <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold text-foreground">
                        {conversation.otherUserName}
                    </p>
                    <p className="truncate text-sm text-muted-foreground">
                        {conversation.courseName}
                    </p>
                </div>
            </div>

            <div className="flex-1 overflow-y-auto">
                <div className="mx-auto max-w-3xl px-4 py-4">
                    {isLoading ? (
                        <div className="flex h-32 items-center justify-center">
                            <LoadingSpinner />
                        </div>
                    ) : messages.length === 0 ? (
                        <p className="py-8 text-center text-sm text-muted-foreground">
                            {t('noMessages')}
                        </p>
                    ) : (
                        <div className="flex flex-col gap-3">
                            {(() => {
                                const result: React.ReactNode[] = [];
                                let lastDateStr = '';

                                messages.forEach((msg) => {
                                    const msgDate = new Date(msg.sentAt);
                                    const dateStr = msgDate.toDateString();

                                    if (dateStr !== lastDateStr) {
                                        result.push(
                                            <div
                                                key={`divider-${dateStr}`}
                                                className="my-3 flex justify-center"
                                            >
                                                <span className="rounded-full bg-muted/60 px-3 py-1 text-xs font-medium text-muted-foreground">
                                                    {formatDateDivider(msg.sentAt)}
                                                </span>
                                            </div>,
                                        );
                                        lastDateStr = dateStr;
                                    }

                                    result.push(<ChatMessage key={msg.id} message={msg} />);
                                });

                                return result;
                            })()}
                            <div ref={bottomRef} />
                        </div>
                    )}
                </div>
            </div>

            <div className="shrink-0 border-t border-border bg-card">
                <div className="mx-auto max-w-3xl">
                    <MessageInput
                        onSend={(content) => sendMutation.mutate(content)}
                        disabled={sendMutation.isPending}
                        className="border-t-0 bg-transparent"
                    />
                </div>
            </div>
        </div>
    );
}
