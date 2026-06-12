import { useEffect } from 'react';
import { Maximize2, Minimize2, RotateCcw, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import { AiChatMessages } from './AiChatMessages';
import { AiChatInput } from './AiChatInput';
import { useAiChat } from '@/hooks/useAiChat';

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
                    'fixed z-[60] flex flex-col overflow-hidden rounded-xl border border-border bg-card shadow-xl',
                    'transition-all duration-300 ease-in-out',
                    isExpanded
                        ? 'bottom-4 right-4 h-[calc(100vh-2rem)] w-[calc(100vw-2rem)]'
                        : 'bottom-[88px] right-6 h-[520px] w-80',
                    !isExpanded && (visible ? 'opacity-100' : 'pointer-events-none opacity-0'),
                )}
            >
                {/* Header */}
                <div className="flex shrink-0 items-center justify-between border-b border-border px-3 py-2.5">
                    <div className="flex items-center gap-3">
                        <div className="grid h-8 w-8 place-items-center rounded-full bg-accent/20 text-sm text-accent">
                            ✨
                        </div>
                        <div>
                            <p className="font-heading text-sm font-semibold leading-none text-foreground">
                                {t('title')}
                            </p>
                            <p className="mt-1 flex items-center gap-1.5 text-[10px] text-muted-foreground">
                                <span className="h-1.5 w-1.5 rounded-full bg-success shadow-[0_0_5px_rgba(var(--success),0.8)]" />
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
                            className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
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
