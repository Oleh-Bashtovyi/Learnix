import { type KeyboardEvent, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Send } from 'lucide-react';
import { CHAT_LIMITS } from '@/const/ui.constants';
import { cn } from '@/utils/cn';

interface MessageInputProps {
    onSend: (content: string) => void;
    disabled?: boolean;
    className?: string;
}

const LINE_HEIGHT = 24;
const MAX_ROWS = 6;
const MAX_HEIGHT = LINE_HEIGHT * MAX_ROWS + 20; // +20 for padding

export function MessageInput({ onSend, disabled, className }: MessageInputProps) {
    const { t } = useTranslation('messages');
    const [value, setValue] = useState('');
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const adjustHeight = () => {
        const el = textareaRef.current;
        if (!el) return;
        el.style.height = 'auto';
        el.style.height = `${Math.min(el.scrollHeight, MAX_HEIGHT)}px`;
        el.style.overflowY = el.scrollHeight > MAX_HEIGHT ? 'auto' : 'hidden';
    };

    useEffect(() => {
        adjustHeight();
    }, [value]);

    const handleSend = () => {
        const trimmed = value.trim();
        if (!trimmed) return;
        onSend(trimmed);
        setValue('');
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    const remaining = CHAT_LIMITS.MESSAGE_MAX - value.length;
    const isNearLimit = remaining <= 200;

    return (
        <div className={cn('flex flex-col gap-1 p-3', className)}>
            <div className="flex items-end gap-2">
                <textarea
                    ref={textareaRef}
                    className={cn(
                        'flex-1 resize-none rounded-lg border border-input bg-transparent px-3 py-2',
                        'text-sm text-foreground placeholder:text-muted-foreground',
                        'focus:outline-none focus:ring-2 focus:ring-ring',
                        'scrollbar-thin scrollbar-track-transparent scrollbar-thumb-border min-h-[40px]',
                    )}
                    style={{ overflowY: 'hidden' }}
                    placeholder={t('typeMessage')}
                    value={value}
                    rows={1}
                    maxLength={CHAT_LIMITS.MESSAGE_MAX}
                    onChange={(e) => setValue(e.target.value)}
                    onKeyDown={handleKeyDown}
                    disabled={disabled}
                />
                <button
                    type="button"
                    onClick={handleSend}
                    disabled={disabled || !value.trim()}
                    className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
                >
                    <Send className="size-4" />
                </button>
            </div>
            {isNearLimit && (
                <p
                    className={cn(
                        'text-right text-xs',
                        remaining <= 0 ? 'text-destructive' : 'text-muted-foreground',
                    )}
                >
                    {remaining}
                </p>
            )}
        </div>
    );
}
