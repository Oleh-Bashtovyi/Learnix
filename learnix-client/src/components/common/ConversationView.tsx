import { useEffect, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { messagesApi } from '@/api/messages.api';
import { ChatMessage } from '@/components/common/ChatMessage';
import { MessageInput } from '@/components/common/MessageInput';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { MESSAGES } from '@/const/localization/messages';
import type { ConversationSummary } from '@/types/message.types';

interface ConversationViewProps {
    conversation: ConversationSummary;
}

export function ConversationView({ conversation }: ConversationViewProps) {
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

    return (
        <div className="flex h-full flex-col">
            <div className="border-b border-border px-4 py-3">
                <p className="font-semibold text-foreground">{conversation.otherUserName}</p>
                <p className="text-sm text-muted-foreground">{conversation.courseName}</p>
            </div>

            <div className="flex-1 overflow-y-auto">
                <div className="mx-auto max-w-3xl px-4 py-4">
                    {isLoading ? (
                        <div className="flex h-32 items-center justify-center">
                            <LoadingSpinner />
                        </div>
                    ) : messages.length === 0 ? (
                        <p className="py-8 text-center text-sm text-muted-foreground">
                            {MESSAGES.NO_MESSAGES}
                        </p>
                    ) : (
                        <div className="flex flex-col gap-3">
                            {messages.map((msg) => (
                                <ChatMessage key={msg.id} message={msg} />
                            ))}
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
