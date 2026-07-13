import { useWatch } from 'react-hook-form';
import { CHAR_COUNTER_WARNING_RATIO } from '@/const/ui.constants';
import { cn } from '@/utils/cn';

interface CharCounterProps {
    /** Field name. FormInput/FormTextarea take it from the `register()` spread. */
    name: string;
    max: number;
    className?: string;
}

/**
 * `{length}/{max}` under a field, toned by how close it is to the limit: muted while there is
 * room, `warning` past CHAR_COUNTER_WARNING_RATIO, `destructive` once full. All three are design
 * tokens, so light/dark is handled for free.
 *
 * Not rendered directly — set `showCharLimit` (plus `maxLength`) on FormInput/FormTextarea.
 *
 * The field is uncontrolled (react-hook-form keeps the value in the DOM via `register`), so the
 * only way to know the current length is to subscribe to the form. useWatch reads `control` off
 * the surrounding <FormProvider>, which is therefore required — and it re-renders just this
 * counter on a keystroke, not the field or the form. It also picks up programmatic writes
 * (`reset`, `setValue`) that never fire a React onChange.
 */
export function CharCounter({ name, max, className }: CharCounterProps) {
    const value = useWatch({ name });
    const length = typeof value === 'string' ? value.length : 0;

    const isFull = length >= max;
    const isNearLimit = max > 0 && length / max >= CHAR_COUNTER_WARNING_RATIO;

    return (
        <span
            className={cn(
                'shrink-0 self-start text-xs tabular-nums transition-colors',
                isFull
                    ? 'font-medium text-destructive'
                    : isNearLimit
                      ? 'text-warning'
                      : 'text-muted-foreground',
                className,
            )}
        >
            {length}/{max}
        </span>
    );
}
