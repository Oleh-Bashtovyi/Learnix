import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Search } from 'lucide-react';
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
    const bottomRef = useRef<HTMLDivElement>(null);

    function getToolLabel(toolName: string): string {
        switch (toolName) {
            case 'search_courses':
            case 'get_categories':
                return t('searching');
            case 'get_platform_info':
                return t('lookingUpInfo');
            default:
                return t('thinking');
        }
    }

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, streamingContent, activeToolName]);

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
        <div className="flex flex-1 flex-col overflow-y-auto overscroll-contain p-3">
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
                            <AiChatMessage key={msg.id} message={msg} isExpanded={isExpanded} />
                        ))}
                        {activeToolName && (
                            <div className="flex items-center gap-1.5 px-2 text-xs text-muted-foreground">
                                <Search size={12} className="animate-pulse" />
                                <span>{getToolLabel(activeToolName)}</span>
                            </div>
                        )}
                        {isTyping && (
                            <div className="flex justify-start">
                                <div
                                    className={cn(
                                        'px-3.5 py-2.5 shadow-sm',
                                        isExpanded
                                            ? 'max-w-full'
                                            : 'max-w-[85%] rounded-2xl rounded-tl-sm border border-border/50 bg-muted',
                                    )}
                                >
                                    <span className="inline-flex items-center gap-1">
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
                                </div>
                            </div>
                        )}
                        {streamingMessage && (
                            <AiChatMessage
                                message={streamingMessage}
                                isStreaming
                                isExpanded={isExpanded}
                            />
                        )}
                    </>
                )}
                <div ref={bottomRef} />
            </div>
        </div>
    );
}
