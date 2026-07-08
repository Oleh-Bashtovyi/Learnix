import React, { forwardRef } from 'react';
import type { FieldError } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { type VariantProps, cva } from 'class-variance-authority';
import { AlertCircle } from 'lucide-react';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

const formInputVariants = cva(
    'w-full rounded-lg border bg-background text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2',
    {
        variants: {
            variant: {
                default: 'border-input px-3 py-2 focus:ring-ring',
                auth: 'border-border px-3.5 py-2.5 placeholder:text-muted-foreground/50 focus:border-primary focus:ring-primary/10',
                muted: 'border-transparent bg-muted/30 px-3 py-2.5 hover:border-primary/50 focus:border-primary focus:bg-background focus:ring-primary',
            },
            hasError: {
                true: 'border-destructive focus:border-destructive focus:ring-destructive/10',
                false: '',
            },
        },
        defaultVariants: {
            variant: 'default',
            hasError: false,
        },
    },
);

interface FormInputProps
    extends React.InputHTMLAttributes<HTMLInputElement>, VariantProps<typeof formInputVariants> {
    label?: React.ReactNode;
    labelRightAction?: React.ReactNode;
    error?: string | FieldError;
    containerClassName?: string;
}

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-001: React Hook Form & Lightweight Wrappers
 */
export const FormInput = forwardRef<HTMLInputElement, FormInputProps>(
    (
        { label, labelRightAction, error, variant, className, containerClassName, id, ...props },
        ref,
    ) => {
        const { t } = useTranslation('zod');
        const errorMessages = getFieldErrors(error);
        const hasError = errorMessages.length > 0;

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
                {(errorMessages.length > 0 || variant === 'auth') && (
                    <div className={cn('mt-1 space-y-1', variant === 'auth' && 'min-h-[20px]')}>
                        {errorMessages.map((msg, idx) => (
                            <p key={idx} className="text-[13px] leading-tight text-destructive">
                                {t(msg)}
                            </p>
                        ))}
                    </div>
                )}
            </div>
        );
    },
);

FormInput.displayName = 'FormInput';
