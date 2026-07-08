import { MarkdownRenderer } from '@/components/common/ui/MarkdownRenderer';
import type { LocalChatMessage } from '@/types/aiChat.types';
import { cn } from '@/utils/cn';

interface AiChatMessageProps {
    message: LocalChatMessage;
    isStreaming?: boolean;
    isExpanded?: boolean;
}

export function AiChatMessage({
    message,
    isStreaming = false,
    isExpanded = false,
}: AiChatMessageProps) {
    const isUser = message.role === 'user';

    return (
        <div className={cn('flex', isUser ? 'justify-end' : 'justify-start')}>
            <div
                className={cn(
                    'min-w-0 px-3.5 py-2.5 text-sm',
                    isUser
                        ? cn(
                              'bg-chat-user-bubble text-chat-user-bubble-foreground shadow-sm',
                              isExpanded
                                  ? 'max-w-[80%] rounded-2xl rounded-tr-sm'
                                  : 'max-w-[85%] rounded-2xl rounded-tr-sm',
                          )
                        : cn(
                              isExpanded
                                  ? 'max-w-full'
                                  : 'max-w-[85%] rounded-2xl rounded-tl-sm border border-border/50 bg-muted text-foreground shadow-sm',
                          ),
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
                    </>
                )}
            </div>
        </div>
    );
}
