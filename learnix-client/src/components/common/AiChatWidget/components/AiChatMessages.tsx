import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Search } from 'lucide-react';
import { AI_CHAT_TOOLS } from '@/const/aiChat.constants';
import type { LocalChatMessage } from '@/types/aiChat.types';
import { cn } from '@/utils/cn';
import { AiChatMessage } from './AiChatMessage';

interface AiChatMessagesProps {
    messages: LocalChatMessage[];
    streamingContent: string;
    isStreaming: boolean;
    activeToolName: string | null;
    isSessionLoading: boolean;
    isExpanded?: boolean;
}

export function AiChatMessages({
    messages,
    streamingContent,
    isStreaming,
    activeToolName,
    isSessionLoading,
    isExpanded = false,
}: AiChatMessagesProps) {
    const { t } = useTranslation('aiChat');
    const scrollRef = useRef<HTMLDivElement>(null);
    // Whether the view is following the stream. A ref, not state: it changes on every scroll event
    // and nothing renders differently because of it.
    const isPinnedRef = useRef(true);

    function getToolLabel(toolName: string): string {
        switch (toolName) {
            case AI_CHAT_TOOLS.searchCourses:
            case AI_CHAT_TOOLS.getCategories:
                return t('searching');
            case AI_CHAT_TOOLS.getPlatformInfo:
                return t('lookingUpInfo');
            case AI_CHAT_TOOLS.getCurrentLesson:
                return t('readingLesson');
            case AI_CHAT_TOOLS.getMyTestReview:
                return t('reviewingAttempt');
            default:
                return t('thinking');
        }
    }

    /** How far off the bottom the user may sit and still count as "following along". */
    const PIN_THRESHOLD_PX = 48;

    function handleScroll() {
        const el = scrollRef.current;
        if (!el) return;
        const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
        isPinnedRef.current = distanceFromBottom <= PIN_THRESHOLD_PX;
    }

    // Following the stream. Instant, not smooth: a token arrives every few tens of milliseconds, and
    // each `behavior: 'smooth'` call would start a fresh animation on top of the one still running —
    // they fight, and the result is the stutter you see. Snapping to the bottom on every token looks
    // perfectly smooth precisely because each step is only a line or two tall.
    //
    // And only while pinned. Scrolling up during a reply means the user wants to read something else;
    // dragging them back down would be taking the scrollbar out of their hands.
    useEffect(() => {
        if (!isPinnedRef.current) return;
        const el = scrollRef.current;
        if (!el) return;
        el.scrollTop = el.scrollHeight;
    }, [streamingContent, activeToolName]);

    // A new message in the list, rather than a token inside one. This is a single large jump, so it
    // gets the smooth animation — there is nothing for it to fight with. A message the user just sent
    // always scrolls into view: they acted, so they expect to see the result, wherever they were.
    const lastMessage = messages[messages.length - 1];
    const isOwnMessage = lastMessage?.role === 'user';

    useEffect(() => {
        const el = scrollRef.current;
        if (!el) return;
        if (isOwnMessage) isPinnedRef.current = true;
        if (!isPinnedRef.current) return;
        el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    }, [messages, isOwnMessage]);

    if (isSessionLoading) {
        return (
            <div className="flex flex-1 items-center justify-center">
                <div className="size-5 animate-spin rounded-full border-2 border-border border-t-primary" />
            </div>
        );
    }

    const isTyping = isStreaming && !streamingContent && !activeToolName;
    const streamingMessage: LocalChatMessage | null =
        isStreaming && streamingContent
            ? { id: '__streaming__', role: 'assistant', content: streamingContent }
            : null;

    const validMessages = messages.filter((msg) => msg.content.trim().length > 0);
    const isEmpty = validMessages.length === 0 && !isStreaming;

    return (
        <div
            ref={scrollRef}
            onScroll={handleScroll}
            className="flex flex-1 flex-col overflow-y-auto overscroll-contain p-3"
        >
            <div
                className={cn(
                    'flex flex-1 flex-col gap-3',
                    isExpanded ? 'mx-auto w-full max-w-3xl' : '',
                )}
            >
                {isEmpty ? (
                    <p className="m-auto px-4 text-center text-xs text-muted-foreground">
                        {t('welcome')}
                    </p>
                ) : (
                    <>
                        {validMessages.map((msg) => (
                            <AiChatMessage key={msg.id} message={msg} />
                        ))}
                        {activeToolName && (
                            <div className="flex items-center gap-1.5 px-2 text-xs text-muted-foreground">
                                <Search size={12} className="animate-pulse" />
                                <span>{getToolLabel(activeToolName)}</span>
                            </div>
                        )}
                        {isTyping && (
                            <span className="inline-flex items-center gap-1 px-0.5 py-1">
                                <span className="size-1.5 animate-pulse rounded-full bg-foreground/40" />
                                <span
                                    className="size-1.5 animate-pulse rounded-full bg-foreground/40"
                                    style={{ animationDelay: '0.2s' }}
                                />
                                <span
                                    className="size-1.5 animate-pulse rounded-full bg-foreground/40"
                                    style={{ animationDelay: '0.4s' }}
                                />
                            </span>
                        )}
                        {streamingMessage && (
                            <AiChatMessage message={streamingMessage} isStreaming />
                        )}
                    </>
                )}
            </div>
        </div>
    );
}
