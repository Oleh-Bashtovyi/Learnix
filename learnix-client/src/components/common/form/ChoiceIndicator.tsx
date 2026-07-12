import { Check } from 'lucide-react';
import { cn } from '@/utils/cn';

type ChoiceTone = 'default' | 'success' | 'destructive';

interface ChoiceIndicatorProps {
    type: 'radio' | 'checkbox';
    /** Ignored in `peer` mode, where the input's own state drives the visual. */
    checked?: boolean;
    /**
     * Read the state from a sibling `<input class="peer">` instead of a prop. Required for inputs registered
     * with React Hook Form: those are uncontrolled, so nobody re-renders when they are ticked — but CSS sees
     * `:checked` regardless.
     */
    peer?: boolean;
    /** Review mode: colour the control by whether the answer turned out right. Ignored in `peer` mode. */
    tone?: ChoiceTone;
    hasError?: boolean;
    className?: string;
}

const TONE_BORDER: Record<ChoiceTone, string> = {
    default: 'border-field-accent bg-field-accent/10',
    success: 'border-success bg-success/10',
    destructive: 'border-destructive bg-destructive/10',
};

const TONE_DOT: Record<ChoiceTone, string> = {
    default: 'bg-field-accent',
    success: 'bg-success',
    destructive: 'bg-destructive',
};

const TONE_CHECK: Record<ChoiceTone, string> = {
    default: 'text-field-accent',
    success: 'text-success',
    destructive: 'text-destructive',
};

const UNCHECKED_BORDER = 'border-field-border bg-transparent group-hover:border-field-focus/60';

/**
 * `peer-checked:` only reaches siblings of the input, and the mark is a *child* of this wrapper — so the
 * wrapper is what carries the rule, and reveals the mark through it.
 */
const PEER_BORDER =
    'peer-checked:border-field-accent peer-checked:bg-field-accent/10 ' +
    'peer-checked:[&>*]:scale-100 ' +
    'peer-focus-visible:ring-2 peer-focus-visible:ring-field-focus/50 ' +
    'peer-disabled:cursor-not-allowed peer-disabled:opacity-50';

const PEER_MARK = 'scale-0';

/**
 * The circle or box of a choice, drawn rather than left to the browser. A native `<input>` styled with
 * `accent-*` only colours its *checked* state — unchecked, the browser paints its own control, which on a
 * dark background is a white disc. So the control is ours, and the real input lives next to it, hidden.
 *
 * Presentational only: pair it with a visually hidden `<input>` (`sr-only`, plus `peer` in `peer` mode) so
 * keyboard and screen readers still get a genuine radio or checkbox.
 */
export function ChoiceIndicator({
    type,
    checked = false,
    peer = false,
    tone = 'default',
    hasError = false,
    className,
}: ChoiceIndicatorProps) {
    const isRadio = type === 'radio';

    let borderClass: string;
    let dotClass: string;
    let checkClass: string;

    if (peer) {
        // The real input drives the state; the indicator only mirrors it via peer-* classes.
        borderClass = cn(UNCHECKED_BORDER, PEER_BORDER);
        dotClass = cn(TONE_DOT.default, PEER_MARK);
        checkClass = cn(TONE_CHECK.default, PEER_MARK);
    } else if (checked) {
        borderClass = TONE_BORDER[tone];
        dotClass = cn('scale-100', TONE_DOT[tone]);
        checkClass = cn('scale-100', TONE_CHECK[tone]);
    } else {
        borderClass = UNCHECKED_BORDER;
        dotClass = 'scale-0 bg-transparent';
        checkClass = 'scale-0';
    }

    return (
        <span
            aria-hidden
            className={cn(
                'relative flex size-5 shrink-0 items-center justify-center border-2 transition-colors',
                isRadio ? 'rounded-full' : 'rounded',
                borderClass,
                hasError && 'border-field-error',
                className,
            )}
        >
            {isRadio ? (
                <span
                    className={cn(
                        'size-2.5 rounded-full transition-transform duration-200',
                        dotClass,
                    )}
                />
            ) : (
                <Check
                    strokeWidth={3}
                    className={cn('size-3 transition-transform duration-200', checkClass)}
                />
            )}
        </span>
    );
}
