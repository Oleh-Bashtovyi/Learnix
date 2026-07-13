import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { BookOpen, TriangleAlert } from 'lucide-react';
import { ChatComposer } from '@/components/common/chat/ChatComposer';
import { AI_CHAT_LIMITS } from '@/const/aiChat.constants';
import type { AiChatController } from '@/hooks/realtime/useAiChat';
import { useAiStatusLabel } from '@/hooks/shared/useAiStatusLabel';
import { cn } from '@/utils/cn';
import { AiChatClearButton } from './AiChatClearButton';
import { AiChatMessages } from './AiChatMessages';

interface AiChatConversationProps {
    chat: AiChatController;
    /** Centre messages in a max-width column — for full-screen and expanded surfaces. */
    isWide?: boolean;
    /** Left side of the toolbar: a title block, a tab strip, ... */
    header?: ReactNode;
    /** Right side of the toolbar, after the "new conversation" button. */
    actions?: ReactNode;
    toolbarClassName?: string;
    /** Title of the lesson the tutor can read. Omit for the platform assistant. */
    lessonTitle?: string;
}

/**
 * The AI chat itself — messages and composer, with an optional toolbar. Renders as a
 * fragment, so the host decides how it is positioned (floating box, docked panel, sheet).
 * Omit both `header` and `actions` when the host already provides its own toolbar.
 */
export function AiChatConversation({
    chat,
    isWide = false,
    header,
    actions,
    toolbarClassName,
    lessonTitle,
}: AiChatConversationProps) {
    const { t } = useTranslation('aiChat');
    const {
        messages,
        streamingContent,
        isStreaming,
        activeToolName,
        isSessionLoading,
        sendMessage,
        isClearing,
        status,
        isAiAvailable,
    } = chat;

    const hasToolbar = header !== undefined || actions !== undefined;
    const { label: statusLabel } = useAiStatusLabel(status);

    return (
        <>
            {hasToolbar && (
                <div
                    className={cn(
                        'flex shrink-0 items-center justify-between gap-2 border-b border-border px-4 py-3',
                        toolbarClassName,
                    )}
                >
                    {header}
                    <div className="flex shrink-0 items-center gap-1.5">
                        <AiChatClearButton chat={chat} />
                        {actions}
                    </div>
                </div>
            )}

            <AiChatMessages
                messages={messages}
                streamingContent={streamingContent}
                isStreaming={isStreaming}
                activeToolName={activeToolName}
                isSessionLoading={isSessionLoading}
                isExpanded={isWide}
            />

            <div
                className={cn(
                    'shrink-0 border-t border-border p-3',
                    isWide && 'mx-auto w-full max-w-3xl border-t-0 p-4 pb-6',
                )}
            >
                {lessonTitle && isAiAvailable && (
                    <p className="mb-2 flex min-w-0 items-center gap-1.5 text-xs text-muted-foreground">
                        <BookOpen className="size-3.5 shrink-0" aria-hidden />
                        <span className="truncate">
                            {t('lessonContext', { title: lessonTitle })}
                        </span>
                    </p>
                )}

                {/* The toolbar carries the same status, but it is hidden on the full-screen surfaces. */}
                {!isAiAvailable && (
                    <p className="mb-2 flex min-w-0 items-center gap-1.5 text-xs text-warning">
                        <TriangleAlert className="size-3.5 shrink-0" aria-hidden />
                        <span className="truncate">{statusLabel}</span>
                    </p>
                )}

                <ChatComposer
                    onSend={sendMessage}
                    placeholder={isAiAvailable ? t('placeholder') : t('unavailable.composer')}
                    disabled={isStreaming || isClearing || !isAiAvailable}
                    maxLength={AI_CHAT_LIMITS.MESSAGE_MAX}
                />
            </div>
        </>
    );
}
