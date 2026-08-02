import { type ReactNode } from 'react';
import { FIELD_ACCENT } from '@/components/common/form/fieldStyles';
import { cn } from '@/utils/cn';

interface RadioOptionProps {
    name: string;
    checked: boolean;
    onClick: () => void;
    label: ReactNode;
    /** Optional trailing content (e.g. a count) rendered opposite the label. */
    rightElement?: ReactNode;
    disabled?: boolean;
}

/**
 * The shared radio control: a custom-drawn circle driven by the `--field-*` accent tokens,
 * with a real (visually hidden) radio input for accessibility. Use this everywhere instead of
 * styling native radios per page.
 */
export function RadioOption({
    name,
    checked,
    onClick,
    label,
    rightElement,
    disabled,
}: RadioOptionProps) {
    return (
        <label
            className={cn(
                'group flex items-center justify-between py-1.5 transition-all',
                disabled ? 'cursor-not-allowed opacity-50' : 'cursor-pointer',
            )}
        >
            <div className="flex items-center gap-3">
                <div
                    className={cn(
                        'relative flex size-5 shrink-0 items-center justify-center rounded-full border-2 transition-colors',
                        checked
                            ? 'border-field-accent bg-field-accent/10'
                            : 'border-field-border group-hover:border-field-focus/60',
                    )}
                >
                    <input
                        type="radio"
                        name={name}
                        checked={checked}
                        onClick={onClick}
                        disabled={disabled}
                        readOnly
                        className={cn('absolute inset-0 cursor-pointer opacity-0', FIELD_ACCENT)}
                    />
                    <div
                        className={cn(
                            'size-2.5 rounded-full transition-all duration-200',
                            checked ? 'scale-100 bg-field-accent' : 'scale-0 bg-transparent',
                        )}
                    />
                </div>
                <span
                    className={cn(
                        'text-sm transition-colors',
                        checked
                            ? 'font-medium text-foreground'
                            : 'text-foreground/80 group-hover:text-foreground',
                    )}
                >
                    {label}
                </span>
            </div>
            {rightElement}
        </label>
    );
}
