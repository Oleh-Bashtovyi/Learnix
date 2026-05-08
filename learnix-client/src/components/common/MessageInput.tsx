import { useState, type KeyboardEvent } from 'react';
import { Send } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/utils/cn';
import { MESSAGES } from '@/const/localization/messages';

interface MessageInputProps {
    onSend: (content: string) => void;
    disabled?: boolean;
    className?: string;
}

export function MessageInput({ onSend, disabled, className }: MessageInputProps) {
    const [value, setValue] = useState('');

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

    return (
        <div className={cn('flex items-end gap-2 border-t border-border bg-card p-3', className)}>
            <textarea
                className={cn(
                    'flex-1 resize-none rounded-lg border border-input bg-background px-3 py-2',
                    'text-sm text-foreground placeholder:text-muted-foreground',
                    'focus:outline-none focus:ring-2 focus:ring-ring',
                    'max-h-[120px] min-h-[40px]',
                )}
                placeholder={MESSAGES.TYPE_MESSAGE}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={disabled}
                rows={1}
            />
            <Button
                size="icon"
                onClick={handleSend}
                disabled={disabled || !value.trim()}
                className="shrink-0"
            >
                <Send className="h-4 w-4" />
            </Button>
        </div>
    );
}
