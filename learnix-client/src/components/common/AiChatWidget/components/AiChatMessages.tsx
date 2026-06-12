import { useEffect, useRef } from 'react';
import { Search } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { AiChatMessage } from './AiChatMessage';
import type { LocalChatMessage } from '@/types/aiChat.types';

interface AiChatMessagesProps {
    messages: LocalChatMessage[];
    streamingContent: string;
    isStreaming: boolean;
    activeToolName: string | null;
    isSessionLoading: boolean;
}

export function AiChatMessages({
    messages,
    streamingContent,
    isStreaming,
    activeToolName,
    isSessionLoading,
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
                <div className="h-5 w-5 animate-spin rounded-full border-2 border-border border-t-primary" />
            </div>
        );
    }

    const streamingMessage: LocalChatMessage | null = isStreaming
        ? { id: '__streaming__', role: 'assistant', content: streamingContent || ' ' }
        : null;

    const isEmpty = messages.length === 0 && !streamingMessage;

    return (
        <div className="flex flex-1 flex-col gap-3 overflow-y-auto p-3">
            {isEmpty ? (
                <p className="m-auto px-4 text-center text-xs text-muted-foreground">
                    {t('welcome')}
                </p>
            ) : (
                <>
                    {messages.map((msg) => (
                        <AiChatMessage key={msg.id} message={msg} />
                    ))}
                    {activeToolName && (
                        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                            <Search size={12} className="animate-pulse" />
                            <span>{getToolLabel(activeToolName)}</span>
                        </div>
                    )}
                    {streamingMessage && <AiChatMessage message={streamingMessage} isStreaming />}
                </>
            )}
            <div ref={bottomRef} />
        </div>
    );
}
