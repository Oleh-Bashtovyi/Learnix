import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Maximize2, Minimize2, X } from 'lucide-react';
import { useAiChat } from '@/hooks/realtime/useAiChat';
import { useMediaQuery } from '@/hooks/shared/useMediaQuery';
import type { ChatScope } from '@/types/aiChat.types';
import { cn } from '@/utils/cn';
import { AiChatConversation } from './AiChatConversation';
import { AiChatStatusLine } from './AiChatStatusLine';

interface AiChatPanelProps {
    isOpen: boolean;
    onClose: () => void;
    isExpanded: boolean;
    onToggleExpand: () => void;
}

const PLATFORM_SCOPE: ChatScope = { kind: 'platform' };

export function AiChatPanel({ isOpen, onClose, isExpanded, onToggleExpand }: AiChatPanelProps) {
    const { t } = useTranslation('aiChat');
    const chat = useAiChat(isOpen, PLATFORM_SCOPE);
    const isMobile = useMediaQuery('(max-width: 639px)');

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
                    // Desktop: Floating box, anchored to the same column-tracking offset as the button.
                    'sm:bottom-[88px] sm:left-auto sm:right-[var(--ai-widget-right)] sm:top-auto sm:h-[520px] sm:w-[400px] sm:rounded-xl sm:border sm:border-border',
                    // Desktop: Expanded overrides
                    isExpanded &&
                        'sm:bottom-4 sm:right-4 sm:h-[calc(100dvh-2rem)] sm:w-[calc(100vw-2rem)]',
                    // Visibility
                    visible
                        ? 'translate-y-0 opacity-100'
                        : 'pointer-events-none translate-y-8 opacity-0 sm:translate-y-4',
                )}
            >
                <AiChatConversation
                    chat={chat}
                    isWide={isExpanded || isMobile}
                    header={
                        <div className="flex min-w-0 items-center gap-3">
                            <div className="grid size-10 shrink-0 place-items-center rounded-full bg-accent/20 text-base text-accent">
                                ✨
                            </div>
                            <div className="min-w-0">
                                <p className="truncate font-heading text-base font-semibold leading-none text-foreground">
                                    {t('title')}
                                </p>
                                <AiChatStatusLine status={chat.status} />
                            </div>
                        </div>
                    }
                    actions={
                        <>
                            <button
                                type="button"
                                onClick={onToggleExpand}
                                aria-label={isExpanded ? t('ariaCollapse') : t('ariaExpand')}
                                className="hidden rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground sm:block"
                            >
                                {isExpanded ? <Minimize2 size={18} /> : <Maximize2 size={18} />}
                            </button>
                            <button
                                type="button"
                                onClick={onClose}
                                aria-label={t('ariaClose')}
                                className="rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                <X size={18} />
                            </button>
                        </>
                    }
                />
            </div>
        </>
    );
}
