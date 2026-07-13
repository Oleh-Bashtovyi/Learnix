import { type ReactNode, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ChevronLeft, ExternalLink } from 'lucide-react';
import { toast } from 'sonner';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { ChatComposer } from '@/components/common/chat/ChatComposer';
import { ChatMessage } from '@/components/common/messaging/ChatMessage';
import { LoadingSpinner } from '@/components/common/ui/LoadingSpinner';
import { MESSAGING_LIMITS } from '@/const/messaging.constants';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import type { ConversationSummary } from '@/types/message.types';

interface ConversationViewProps {
    conversation: ConversationSummary;
    onBack?: () => void;
    /** Rendered at the right edge of the header — e.g. a close button. */
    headerActions?: ReactNode;
}

export function ConversationView({ conversation, onBack, headerActions }: ConversationViewProps) {
    const { t } = useTranslation('messages');
    const user = useAuthStore((s) => s.user);
    const queryClient = useQueryClient();
    const bottomRef = useRef<HTMLDivElement>(null);

    // Only an instructor has a public profile. Derived here rather than passed in as a prop — which is
    // what it used to be, and which no caller ever set, so the link never appeared at all. Deriving it
    // from the conversation also means no call site can wire up a link to a student by mistake: an
    // instructor following it would land on a page built to show courses taught, students and ratings,
    // for somebody who has none of those and never agreed to be looked up that way.
    const profileHref = conversation.otherUserIsInstructor
        ? APP_ROUTES.public.instructorProfile(conversation.otherUserId)
        : undefined;

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

    function handleSend(content: string) {
        if (!user?.emailVerified) {
            toast.error(t('emailNotVerified', 'Please confirm your email address first.'));
            return;
        }
        sendMutation.mutate(content);
    }

    const formatDateDivider = (dateString: string) => {
        const date = new Date(dateString);
        const today = new Date();
        const yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);

        if (date.toDateString() === today.toDateString()) {
            return t('common:general.today');
        }
        if (date.toDateString() === yesterday.toDateString()) {
            return t('common:general.yesterday');
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
                        className="flex size-9 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted md:hidden"
                        aria-label="Back to conversations"
                    >
                        <ChevronLeft className="size-5" />
                    </button>
                )}
                <div className="min-w-0 flex-1">
                    {profileHref ? (
                        <Link
                            to={profileHref}
                            className="group flex items-center gap-1 truncate font-semibold text-foreground transition-colors hover:text-primary"
                        >
                            <span className="truncate">{conversation.otherUserName}</span>
                            <ExternalLink className="size-3.5 shrink-0 opacity-0 transition-opacity group-hover:opacity-100" />
                        </Link>
                    ) : (
                        <p className="truncate font-semibold text-foreground">
                            {conversation.otherUserName}
                        </p>
                    )}
                    <p className="truncate text-sm text-muted-foreground">
                        {conversation.courseName}
                    </p>
                </div>

                {headerActions && (
                    <div className="flex shrink-0 items-center gap-1.5">{headerActions}</div>
                )}
            </div>

            <div className="flex-1 overflow-y-auto">
                <div className="mx-auto max-w-3xl p-4">
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
                <div className="mx-auto max-w-3xl p-3">
                    <ChatComposer
                        onSend={handleSend}
                        placeholder={t('typeMessage')}
                        disabled={sendMutation.isPending}
                        maxLength={MESSAGING_LIMITS.MESSAGE_MAX}
                    />
                </div>
            </div>
        </div>
    );
}
