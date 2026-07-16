import React, { forwardRef } from 'react';
import { type VariantProps, cva } from 'class-variance-authority';
import { CharCounter } from '@/components/common/form/CharCounter';
import { FIELD_BASE, FIELD_ERROR, FIELD_SURFACE_CARD } from '@/components/common/form/fieldStyles';
import { CHAR_COUNTER_REVEAL_RATIO } from '@/const/ui.constants';
import { cn } from '@/utils/cn';

// Border/focus/fill come from the shared field tokens; the only choice is the surface.
const formTextareaVariants = cva(`${FIELD_BASE} resize-none px-3 py-2.5`, {
    variants: {
        variant: {
            default: '',
            card: FIELD_SURFACE_CARD,
        },
        hasError: {
            true: FIELD_ERROR,
            false: '',
        },
    },
    defaultVariants: {
        variant: 'default',
        hasError: false,
    },
});

interface FormTextareaProps
    extends
        React.TextareaHTMLAttributes<HTMLTextAreaElement>,
        VariantProps<typeof formTextareaVariants> {
    label?: React.ReactNode;
    /** Rendered opposite the label — e.g. a secondary action. */
    labelRightAction?: React.ReactNode;
    error?: string;
    /**
     * Show a `{length}/{maxLength}` counter under the field. Needs `maxLength`, and the form must
     * be wrapped in a react-hook-form <FormProvider> — that is where the counter reads the value.
     *
     * `"nearLimit"` holds the counter back until the value is within CHAR_COUNTER_REVEAL_RATIO of
     * the limit — for a ceiling set high enough that reaching it is not a real prospect, and whose
     * counter would otherwise just sit there reading `0/1000`.
     */
    showCharLimit?: boolean | 'nearLimit';
    containerClassName?: string;
}

export const FormTextarea = forwardRef<HTMLTextAreaElement, FormTextareaProps>(
    (
        {
            label,
            labelRightAction,
            error,
            showCharLimit,
            variant,
            className,
            containerClassName,
            id,
            name,
            maxLength,
            ...props
        },
        ref,
    ) => {
        const hasError = !!error;
        const counterMax = showCharLimit && name ? maxLength : undefined;
        return (
            <div className={cn('space-y-1', containerClassName)}>
                {(label || labelRightAction) && (
                    <div className="flex items-center justify-between gap-2">
                        {label ? (
                            <label htmlFor={id} className="text-sm font-medium text-foreground">
                                {label}
                            </label>
                        ) : (
                            <span />
                        )}
                        {labelRightAction}
                    </div>
                )}
                <textarea
                    id={id}
                    ref={ref}
                    name={name}
                    maxLength={maxLength}
                    className={cn(formTextareaVariants({ variant, hasError, className }))}
                    {...props}
                />
                {/* One row so the counter sits beside the error instead of pushing it around.
                    `empty:hidden` is what keeps a withheld "nearLimit" counter from leaving a gap:
                    only CharCounter knows whether it is showing, so the row is rendered either way
                    and collapses itself once both slots come back with nothing. That depends on
                    CharCounter being this row's direct child — do not wrap it. */}
                {(hasError || counterMax !== undefined) && (
                    <div className="flex items-start gap-3 empty:hidden">
                        {error && (
                            <p className="min-w-0 flex-1 text-sm text-destructive">{error}</p>
                        )}
                        {counterMax !== undefined && name && (
                            <CharCounter
                                name={name}
                                max={counterMax}
                                className="ml-auto"
                                revealAt={
                                    showCharLimit === 'nearLimit'
                                        ? CHAR_COUNTER_REVEAL_RATIO
                                        : undefined
                                }
                            />
                        )}
                    </div>
                )}
            </div>
        );
    },
);

FormTextarea.displayName = 'FormTextarea';
