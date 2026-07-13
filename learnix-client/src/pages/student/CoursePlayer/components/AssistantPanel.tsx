import { useTranslation } from 'react-i18next';
import { MessageSquare, Sparkles, X } from 'lucide-react';
import { AiChatClearButton } from '@/components/common/AiChatWidget/components/AiChatClearButton';
import { AiChatConversation } from '@/components/common/AiChatWidget/components/AiChatConversation';
import { AiChatStatusLine } from '@/components/common/AiChatWidget/components/AiChatStatusLine';
import { ConversationView } from '@/components/common/messaging/ConversationView';
import type { AiChatController } from '@/hooks/realtime/useAiChat';
import type { ConversationSummary } from '@/types/message.types';
import { cn } from '@/utils/cn';

export type AssistantTab = 'ai' | 'instructor';

interface AssistantPanelProps {
    activeTab: AssistantTab;
    onTabChange: (tab: AssistantTab) => void;
    onClose: () => void;
    chat: AiChatController;
    conversation: ConversationSummary | null;
    isConversationLoading: boolean;
    /** Shown above the composer so the student knows which lesson the tutor can read. */
    lessonTitle?: string;
    /** Mobile sheet — it covers the page header, so the panel has to carry its own tabs. */
    isFullScreen?: boolean;
}

const TOOLBAR_CLASS = 'flex shrink-0 items-center justify-between gap-2 border-b border-border';

/**
 * The right-hand companion panel of the course player: the AI tutor and the
 * instructor conversation, sharing one surface. Both tabs stay mounted so
 * switching never drops an in-flight AI stream or a loaded thread.
 *
 * Docked, the page header's buttons are the tab switcher — a second one here would
 * duplicate them. Full-screen, they are hidden behind the sheet, so tabs move inside.
 */
export function AssistantPanel({
    activeTab,
    onTabChange,
    onClose,
    chat,
    conversation,
    isConversationLoading,
    lessonTitle,
    isFullScreen = false,
}: AssistantPanelProps) {
    const { t } = useTranslation('lessonPlayer');
    const { t: tAi } = useTranslation('aiChat');

    const closeButton = (
        <button
            type="button"
            onClick={onClose}
            aria-label={t('assistant.close')}
            title={t('assistant.close')}
            className="rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
        >
            <X size={18} />
        </button>
    );

    const aiTitle = (
        <div className="flex min-w-0 items-center gap-3">
            <div className="grid size-9 shrink-0 place-items-center rounded-full bg-accent/20 text-sm text-accent">
                ✨
            </div>
            <div className="min-w-0">
                <p className="truncate font-heading text-sm font-semibold leading-none text-foreground">
                    {tAi('title')}
                </p>
                <AiChatStatusLine status={chat.status} />
            </div>
        </div>
    );

    return (
        <div className="flex h-full min-h-0 flex-col bg-card">
            {isFullScreen && (
                <div className={cn(TOOLBAR_CLASS, 'px-3 py-2')}>
                    <TabStrip activeTab={activeTab} onTabChange={onTabChange} />
                    <div className="flex shrink-0 items-center gap-1.5">
                        {activeTab === 'ai' && <AiChatClearButton chat={chat} />}
                        {closeButton}
                    </div>
                </div>
            )}

            <div className={cn('flex min-h-0 flex-1 flex-col', activeTab !== 'ai' && 'hidden')}>
                <AiChatConversation
                    chat={chat}
                    isWide={isFullScreen}
                    header={isFullScreen ? undefined : aiTitle}
                    actions={isFullScreen ? undefined : closeButton}
                    lessonTitle={lessonTitle}
                />
            </div>

            <div
                className={cn(
                    'flex min-h-0 flex-1 flex-col',
                    activeTab !== 'instructor' && 'hidden',
                )}
            >
                {conversation ? (
                    <div className="min-h-0 flex-1">
                        <ConversationView
                            conversation={conversation}
                            headerActions={isFullScreen ? undefined : closeButton}
                        />
                    </div>
                ) : (
                    <>
                        {!isFullScreen && (
                            <div className={cn(TOOLBAR_CLASS, 'px-4 py-3')}>
                                <p className="truncate font-heading text-sm font-semibold text-foreground">
                                    {t('assistant.tabs.instructor')}
                                </p>
                                {closeButton}
                            </div>
                        )}
                        <div className="flex flex-1 items-center justify-center p-6 text-center">
                            {isConversationLoading ? (
                                <div className="size-5 animate-spin rounded-full border-2 border-border border-t-primary" />
                            ) : (
                                <p className="text-sm text-muted-foreground">
                                    {t('assistant.instructorUnavailable')}
                                </p>
                            )}
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}

const TABS = [
    { id: 'ai', icon: Sparkles, labelKey: 'assistant.tabs.ai' },
    { id: 'instructor', icon: MessageSquare, labelKey: 'assistant.tabs.instructor' },
] as const;

function TabStrip({
    activeTab,
    onTabChange,
}: Pick<AssistantPanelProps, 'activeTab' | 'onTabChange'>) {
    const { t } = useTranslation('lessonPlayer');

    return (
        <div role="tablist" className="flex min-w-0 items-center gap-0.5 rounded-lg bg-muted p-0.5">
            {TABS.map(({ id, icon: Icon, labelKey }) => {
                const isActive = activeTab === id;
                return (
                    <button
                        key={id}
                        type="button"
                        role="tab"
                        aria-selected={isActive}
                        onClick={() => onTabChange(id)}
                        className={cn(
                            'flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-medium transition-colors',
                            isActive
                                ? 'bg-card text-foreground shadow-sm'
                                : 'text-muted-foreground hover:text-foreground',
                        )}
                    >
                        <Icon className="size-3.5 shrink-0" />
                        <span className="truncate">{t(labelKey)}</span>
                    </button>
                );
            })}
        </div>
    );
}
