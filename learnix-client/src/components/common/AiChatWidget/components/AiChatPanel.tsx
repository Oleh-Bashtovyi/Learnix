import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Maximize2, Minimize2, RotateCcw, X } from 'lucide-react';
import { useAiChat } from '@/hooks/realtime/useAiChat';
import { cn } from '@/utils/cn';
import { AiChatInput } from './AiChatInput';
import { AiChatMessages } from './AiChatMessages';

interface AiChatPanelProps {
    isOpen: boolean;
    onClose: () => void;
    isExpanded: boolean;
    onToggleExpand: () => void;
}

export function AiChatPanel({ isOpen, onClose, isExpanded, onToggleExpand }: AiChatPanelProps) {
    const { t } = useTranslation('aiChat');
    const {
        messages,
        streamingContent,
        isStreaming,
        activeToolName,
        isSessionLoading,
        sendMessage,
        clearSession,
        isClearing,
    } = useAiChat(isOpen);

    useEffect(() => {
        document.body.style.overflow = isExpanded ? 'hidden' : '';
        return () => {
            document.body.style.overflow = '';
        };
    }, [isExpanded]);

    const visible = isOpen || isExpanded;

    return (
        <>
            {/* Backdrop */}
            <div
                className={cn(
                    'fixed inset-0 z-[59] bg-black/50 transition-opacity duration-300',
                    isExpanded ? 'opacity-100' : 'pointer-events-none opacity-0',
                )}
            />

            <div
                className={cn(
                    'fixed z-[60] flex flex-col overflow-hidden bg-card shadow-xl transition-all duration-300 ease-in-out',
                    // Mobile: Always full screen
                    'inset-0 rounded-none border-0',
                    // Desktop: Floating box
                    'sm:bottom-[88px] sm:left-auto sm:right-6 sm:top-auto sm:h-[520px] sm:w-80 sm:rounded-xl sm:border sm:border-border',
                    // Desktop: Expanded overrides
                    isExpanded &&
                        'sm:bottom-4 sm:right-4 sm:h-[calc(100vh-2rem)] sm:w-[calc(100vw-2rem)]',
                    // Visibility
                    visible
                        ? 'translate-y-0 opacity-100'
                        : 'pointer-events-none translate-y-8 opacity-0 sm:translate-y-4',
                )}
            >
                {/* Header */}
                <div className="flex shrink-0 items-center justify-between border-b border-border px-3 py-2.5">
                    <div className="flex items-center gap-3">
                        <div className="grid size-8 place-items-center rounded-full bg-accent/20 text-sm text-accent">
                            ✨
                        </div>
                        <div>
                            <p className="font-heading text-sm font-semibold leading-none text-foreground">
                                {t('title')}
                            </p>
                            <p className="mt-1 flex items-center gap-1.5 text-[10px] text-muted-foreground">
                                <span className="size-1.5 rounded-full bg-success shadow-[0_0_5px_rgba(var(--success),0.8)]" />
                                {t('status', { defaultValue: 'Active · Ready to help' })}
                            </p>
                        </div>
                    </div>
                    <div className="flex items-center gap-1">
                        <button
                            onClick={() => clearSession()}
                            disabled={isClearing || isStreaming || messages.length === 0}
                            title={t('ariaClear')}
                            aria-label={t('ariaClear')}
                            className={cn(
                                'rounded-md p-1 text-muted-foreground transition-colors',
                                'hover:bg-secondary hover:text-foreground',
                                'disabled:pointer-events-none disabled:opacity-40',
                            )}
                        >
                            <RotateCcw size={14} />
                        </button>
                        <button
                            onClick={onToggleExpand}
                            aria-label={isExpanded ? t('ariaCollapse') : t('ariaExpand')}
                            className="hidden rounded-md p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground sm:block"
                        >
                            {isExpanded ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
                        </button>
                        <button
                            onClick={onClose}
                            aria-label={t('ariaClose')}
                            className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                        >
                            <X size={14} />
                        </button>
                    </div>
                </div>

                <AiChatMessages
                    messages={messages}
                    streamingContent={streamingContent}
                    isStreaming={isStreaming}
                    activeToolName={activeToolName}
                    isSessionLoading={isSessionLoading}
                />

                <AiChatInput onSend={sendMessage} disabled={isStreaming || isClearing} />
            </div>
        </>
    );
}
