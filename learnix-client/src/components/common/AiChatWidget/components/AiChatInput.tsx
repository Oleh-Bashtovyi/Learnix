import { type KeyboardEvent, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { SendHorizontal } from 'lucide-react';
import { cn } from '@/utils/cn';

interface AiChatInputProps {
    onSend: (message: string) => void;
    disabled?: boolean;
    isExpanded?: boolean;
}

export function AiChatInput({ onSend, disabled = false, isExpanded = false }: AiChatInputProps) {
    const { t } = useTranslation('aiChat');
    const [value, setValue] = useState('');
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const handleSend = () => {
        const trimmed = value.trim();
        if (!trimmed || disabled) return;
        onSend(trimmed);
        setValue('');
        if (textareaRef.current) {
            textareaRef.current.style.height = 'auto';
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    const handleInput = () => {
        const el = textareaRef.current;
        if (!el) return;
        el.style.height = 'auto';
        el.style.height = `${Math.min(el.scrollHeight, 120)}px`;
    };

    return (
        <div
            className={cn(
                'border-t border-border p-2',
                isExpanded ? 'mx-auto w-full max-w-3xl border-t-0 p-4 pb-6' : '',
            )}
        >
            <div className="flex items-end gap-2 rounded-lg border border-border bg-background px-3 py-2 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary">
                <textarea
                    ref={textareaRef}
                    rows={1}
                    value={value}
                    onChange={(e) => setValue(e.target.value)}
                    onKeyDown={handleKeyDown}
                    onInput={handleInput}
                    placeholder={t('placeholder')}
                    disabled={disabled}
                    className="flex-1 resize-none bg-transparent py-[2px] text-sm text-foreground placeholder:text-muted-foreground focus:outline-none disabled:opacity-50"
                />
                <button
                    onClick={handleSend}
                    disabled={disabled || !value.trim()}
                    aria-label={t('common:actions.send')}
                    className={cn(
                        'shrink-0 rounded-md p-1 transition-colors',
                        'text-muted-foreground hover:text-primary',
                        'disabled:pointer-events-none disabled:opacity-40',
                    )}
                >
                    <SendHorizontal size={16} />
                </button>
            </div>
        </div>
    );
}
