import type { ButtonHTMLAttributes } from 'react';
import { TEXT_LINK_BASE } from '@/components/common/ui/textLinkStyles';
import { cn } from '@/utils/cn';

type TextButtonProps = ButtonHTMLAttributes<HTMLButtonElement>;

/**
 * Reads as a link, behaves as a button. Use it where the affordance belongs beside a label — the
 * shape a TextLink occupies — but the click runs an action rather than navigating anywhere.
 */
export function TextButton({ className, children, type = 'button', ...props }: TextButtonProps) {
    return (
        <button
            type={type}
            className={cn(TEXT_LINK_BASE, 'disabled:opacity-50', className)}
            {...props}
        >
            {children}
        </button>
    );
}
