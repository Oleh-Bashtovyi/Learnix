import React, { forwardRef } from 'react';
import type { FieldError } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { type VariantProps, cva } from 'class-variance-authority';
import { AlertCircle } from 'lucide-react';
import { CharCounter } from '@/components/common/form/CharCounter';
import { FIELD_BASE, FIELD_ERROR, FIELD_SURFACE_CARD } from '@/components/common/form/fieldStyles';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

// Border/focus/fill come from the shared field tokens. The only choice a caller makes is the
// surface: `default` (on the page) or `card` (inside a card) — never border/focus colours.
const formInputVariants = cva(`${FIELD_BASE} px-3 py-2.5`, {
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

interface FormInputProps
    extends React.InputHTMLAttributes<HTMLInputElement>, VariantProps<typeof formInputVariants> {
    label?: React.ReactNode;
    labelRightAction?: React.ReactNode;
    error?: string | FieldError;
    /**
     * Show a `{length}/{maxLength}` counter under the field. Needs `maxLength`, and the form must
     * be wrapped in a react-hook-form <FormProvider> — that is where the counter reads the value.
     */
    showCharLimit?: boolean;
    containerClassName?: string;
    /**
     * Keep a fixed-height slot under the field even when there is no error, so the form does
     * not shift as validation messages appear/disappear. Opt-in — set it on layouts where the
     * jump is noticeable (e.g. auth forms), not by default.
     */
    reserveErrorSpace?: boolean;
}

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-001: React Hook Form & Lightweight Wrappers
 */
export const FormInput = forwardRef<HTMLInputElement, FormInputProps>(
    (
        {
            label,
            labelRightAction,
            error,
            showCharLimit,
            variant,
            className,
            containerClassName,
            reserveErrorSpace,
            id,
            name,
            maxLength,
            ...props
        },
        ref,
    ) => {
        const { t } = useTranslation('zod');
        const errorMessages = getFieldErrors(error);
        const hasError = errorMessages.length > 0;
        const counterMax = showCharLimit && name ? maxLength : undefined;

        return (
            <div className={cn('space-y-1', containerClassName)}>
                {(label || labelRightAction) && (
                    <div className="flex items-center justify-between">
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
                <div className="relative">
                    <input
                        id={id}
                        ref={ref}
                        name={name}
                        maxLength={maxLength}
                        className={cn(
                            formInputVariants({ variant, hasError, className }),
                            hasError && 'pr-10',
                        )}
                        {...props}
                    />
                    {hasError && (
                        <div className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-destructive">
                            <AlertCircle size={18} />
                        </div>
                    )}
                </div>
                {/* One row so the counter sits beside the first error instead of pushing it around;
                    any further messages stack under it in the left column. */}
                {(errorMessages.length > 0 || reserveErrorSpace || counterMax !== undefined) && (
                    <div className="mt-1 flex items-start gap-3">
                        <div
                            className={cn(
                                'min-w-0 flex-1 space-y-1',
                                reserveErrorSpace && 'min-h-[20px]',
                            )}
                        >
                            {errorMessages.map((msg, idx) => (
                                <p key={idx} className="text-[13px] leading-tight text-destructive">
                                    {t(msg)}
                                </p>
                            ))}
                        </div>
                        {counterMax !== undefined && name && (
                            <CharCounter name={name} max={counterMax} />
                        )}
                    </div>
                )}
            </div>
        );
    },
);

FormInput.displayName = 'FormInput';
