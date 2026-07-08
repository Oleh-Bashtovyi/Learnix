import type { MessageItem } from '@/types/message.types';
import { cn } from '@/utils/cn';

interface ChatMessageProps {
    message: MessageItem;
}

export function ChatMessage({ message }: ChatMessageProps) {
    const time = new Date(message.sentAt).toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit',
    });

    return (
        <div
            className={cn(
                'flex gap-2',
                message.isFromCurrentUser ? 'flex-row-reverse' : 'flex-row',
            )}
        >
            <div
                className={cn(
                    'max-w-[70%] rounded-2xl px-4 py-2 text-sm',
                    message.isFromCurrentUser
                        ? 'rounded-tr-sm bg-chat-user-bubble text-chat-user-bubble-foreground'
                        : 'rounded-tl-sm bg-muted text-foreground',
                )}
            >
                {!message.isFromCurrentUser && (
                    <p className="mb-1 text-xs font-medium text-muted-foreground">
                        {message.senderName}
                    </p>
                )}
                <p className="break-words">{message.content}</p>
                <p
                    className={cn(
                        'mt-1 text-right text-xs',
                        message.isFromCurrentUser
                            ? 'text-chat-user-bubble-foreground/70'
                            : 'text-muted-foreground',
                    )}
                >
                    {time}
                </p>
            </div>
        </div>
    );
}
