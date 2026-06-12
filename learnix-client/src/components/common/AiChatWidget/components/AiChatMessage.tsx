import { cn } from '@/utils/cn';
import { MarkdownRenderer } from '@/components/common/MarkdownRenderer';
import type { LocalChatMessage } from '@/types/aiChat.types';

interface AiChatMessageProps {
    message: LocalChatMessage;
    isStreaming?: boolean;
}

export function AiChatMessage({ message, isStreaming = false }: AiChatMessageProps) {
    const isUser = message.role === 'user';

    return (
        <div className={cn('flex', isUser ? 'justify-end' : 'justify-start')}>
            <div
                className={cn(
                    'max-w-[85%] rounded-2xl px-3.5 py-2.5 text-sm shadow-sm',
                    isUser
                        ? 'rounded-tr-sm bg-primary text-primary-foreground'
                        : 'rounded-tl-sm border border-border/50 bg-muted text-foreground',
                )}
            >
                {isUser ? (
                    <p className="whitespace-pre-wrap break-words">{message.content}</p>
                ) : (
                    <>
                        <MarkdownRenderer
                            content={message.content}
                            className={cn(
                                'prose-sm break-words',
                                'prose-p:my-1 prose-ul:my-1 prose-ol:my-1',
                                'prose-li:my-0 prose-headings:my-1',
                                'prose-pre:bg-muted-foreground/10 prose-pre:p-2 prose-pre:rounded',
                                'prose-code:bg-muted-foreground/10 prose-code:px-1 prose-code:rounded prose-code:text-xs',
                                'prose-strong:text-foreground prose-em:text-foreground',
                                '[&>*:first-child]:mt-0 [&>*:last-child]:mb-0',
                            )}
                        />
                        {isStreaming && (
                            <span className="inline-block h-3.5 w-0.5 translate-y-0.5 animate-pulse bg-foreground/60" />
                        )}
                    </>
                )}
            </div>
        </div>
    );
}
