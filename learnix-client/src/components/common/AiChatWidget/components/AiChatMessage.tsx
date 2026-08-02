import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { MarkdownRenderer } from '@/components/common/elements/MarkdownRenderer';
import type { LocalChatMessage } from '@/types/aiChat.types';
import { cn } from '@/utils/cn';

interface AiChatMessageProps {
    message: LocalChatMessage;
    isStreaming?: boolean;
}

/**
 * The student speaks in a bubble, the assistant speaks in the page — the same shape on every surface, from
 * the floating widget to the lesson sidebar. An assistant turn is long and made of headings, lists and code
 * blocks; a bubble fights that markdown for the same edge and shrinks it into a corner for no gain.
 */
export function AiChatMessage({ message, isStreaming = false }: AiChatMessageProps) {
    const { t } = useTranslation('aiChat');

    if (message.role === 'user') {
        return (
            <div className="flex justify-end">
                <div className="min-w-0 max-w-[85%] rounded-2xl rounded-tr-sm bg-chat-user-bubble px-3.5 py-2.5 text-sm text-chat-user-bubble-foreground shadow-sm">
                    <p className="whitespace-pre-wrap break-words">{message.content}</p>
                </div>
            </div>
        );
    }

    return (
        <div className="min-w-0 p-0.5 text-sm text-foreground">
            <MarkdownRenderer
                content={message.content}
                className={cn(
                    'prose-sm break-words',
                    'prose-p:my-1 prose-ol:my-1 prose-ul:my-1',
                    'prose-headings:my-1 prose-li:my-0',
                    'prose-pre:m-0 prose-pre:bg-transparent prose-pre:p-0',
                    'prose-code:rounded-md prose-code:bg-muted-foreground/15 prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:font-medium prose-code:text-foreground prose-code:before:content-none prose-code:after:content-none',
                    'prose-blockquote:my-2 prose-blockquote:border-none prose-blockquote:pl-0 prose-blockquote:not-italic prose-blockquote:text-muted-foreground',
                    'prose-strong:font-bold prose-strong:text-foreground prose-em:italic prose-em:text-muted-foreground',
                    '[&>*:first-child]:mt-0 [&>*:last-child]:mb-0',
                )}
            />
            {isStreaming && (
                <span className="inline-block h-3.5 w-0.5 translate-y-0.5 animate-pulse bg-foreground/60" />
            )}
            {message.truncated && !isStreaming && (
                <p className="mt-2 flex items-center gap-1.5 text-xs text-warning">
                    <AlertTriangle size={12} className="shrink-0" />
                    {t('truncated')}
                </p>
            )}
        </div>
    );
}
