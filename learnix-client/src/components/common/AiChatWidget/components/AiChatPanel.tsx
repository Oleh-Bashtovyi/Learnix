import { RotateCcw, X } from 'lucide-react';
import { cn } from '@/utils/cn';
import { AI_CHAT } from '@/const/localization/aiChat';
import { AiChatMessages } from './AiChatMessages';
import { AiChatInput } from './AiChatInput';
import { useAiChat } from '@/hooks/useAiChat';

interface AiChatPanelProps {
    isOpen: boolean;
    onClose: () => void;
}

export function AiChatPanel({ isOpen, onClose }: AiChatPanelProps) {
    const {
        messages,
        streamingContent,
        isStreaming,
        isSearching,
        isSessionLoading,
        sendMessage,
        clearSession,
        isClearing,
    } = useAiChat(isOpen);

    return (
        <div
            className={cn(
                'absolute bottom-16 right-0 flex w-80 flex-col rounded-xl border border-border bg-card shadow-xl',
                'origin-bottom-right transition-all duration-200',
                isOpen
                    ? 'pointer-events-auto scale-100 opacity-100'
                    : 'pointer-events-none scale-95 opacity-0',
            )}
            style={{ height: '520px' }}
        >
            {/* Header */}
            <div className="flex items-center justify-between border-b border-border px-3 py-2.5">
                <div>
                    <p className="font-heading text-sm font-semibold text-foreground">
                        {AI_CHAT.TITLE}
                    </p>
                </div>
                <div className="flex items-center gap-1">
                    <button
                        onClick={() => clearSession()}
                        disabled={isClearing || isStreaming || messages.length === 0}
                        title={AI_CHAT.ARIA_CLEAR}
                        aria-label={AI_CHAT.ARIA_CLEAR}
                        className={cn(
                            'rounded-md p-1 text-muted-foreground transition-colors',
                            'hover:bg-secondary hover:text-foreground',
                            'disabled:pointer-events-none disabled:opacity-40',
                        )}
                    >
                        <RotateCcw size={14} />
                    </button>
                    <button
                        onClick={onClose}
                        aria-label={AI_CHAT.ARIA_CLOSE}
                        className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                    >
                        <X size={14} />
                    </button>
                </div>
            </div>

            {/* Messages */}
            <AiChatMessages
                messages={messages}
                streamingContent={streamingContent}
                isStreaming={isStreaming}
                isSearching={isSearching}
                isSessionLoading={isSessionLoading}
            />

            {/* Input */}
            <AiChatInput onSend={sendMessage} disabled={isStreaming || isClearing} />
        </div>
    );
}
