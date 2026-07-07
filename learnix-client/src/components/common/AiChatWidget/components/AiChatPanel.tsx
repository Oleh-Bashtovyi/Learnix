import { useEffect, useState } from 'react';
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

    const [isMobile, setIsMobile] = useState(() => {
        if (typeof window !== 'undefined') {
            return window.matchMedia('(max-width: 639px)').matches;
        }
        return false;
    });

    useEffect(() => {
        const mql = window.matchMedia('(max-width: 639px)');
        const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches);
        mql.addEventListener('change', handler);
        return () => mql.removeEventListener('change', handler);
    }, []);

    useEffect(() => {
        const shouldLock = isExpanded || (isOpen && isMobile);
        document.body.style.overflow = shouldLock ? 'hidden' : '';
        return () => {
            document.body.style.overflow = '';
        };
    }, [isExpanded, isOpen, isMobile]);

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
                    'sm:bottom-[88px] sm:left-auto sm:right-6 sm:top-auto sm:h-[520px] sm:w-[400px] sm:rounded-xl sm:border sm:border-border',
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
                <div className="flex shrink-0 items-center justify-between border-b border-border px-4 py-3">
                    <div className="flex items-center gap-3">
                        <div className="grid size-10 place-items-center rounded-full bg-accent/20 text-base text-accent">
                            ✨
                        </div>
                        <div>
                            <p className="font-heading text-base font-semibold leading-none text-foreground">
                                {t('common:navigation.myLearning')}
                            </p>
                            <p className="mt-1.5 flex items-center gap-1.5 text-xs text-muted-foreground">
                                <span className="size-2 rounded-full bg-success shadow-[0_0_5px_rgba(var(--success),0.8)]" />
                                {t('status', { defaultValue: 'Active · Ready to help' })}
                            </p>
                        </div>
                    </div>
                    <div className="flex items-center gap-1.5">
                        <button
                            onClick={() => clearSession()}
                            disabled={isClearing || isStreaming || messages.length === 0}
                            title={t('ariaClear')}
                            aria-label={t('ariaClear')}
                            className={cn(
                                'rounded-md p-1.5 text-muted-foreground transition-colors',
                                'hover:bg-secondary hover:text-foreground',
                                'disabled:pointer-events-none disabled:opacity-40',
                            )}
                        >
                            <RotateCcw size={18} />
                        </button>
                        <button
                            onClick={onToggleExpand}
                            aria-label={isExpanded ? t('ariaCollapse') : t('ariaExpand')}
                            className="hidden rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground sm:block"
                        >
                            {isExpanded ? <Minimize2 size={18} /> : <Maximize2 size={18} />}
                        </button>
                        <button
                            onClick={onClose}
                            aria-label={t('ariaClose')}
                            className="rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                        >
                            <X size={18} />
                        </button>
                    </div>
                </div>

                <AiChatMessages
                    messages={messages}
                    streamingContent={streamingContent}
                    isStreaming={isStreaming}
                    activeToolName={activeToolName}
                    isSessionLoading={isSessionLoading}
                    isExpanded={isExpanded || isMobile}
                />

                <AiChatInput
                    onSend={sendMessage}
                    disabled={isStreaming || isClearing}
                    isExpanded={isExpanded || isMobile}
                />
            </div>
        </>
    );
}
