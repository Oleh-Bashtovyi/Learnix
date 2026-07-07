import React, { forwardRef } from 'react';
import type { FieldError } from 'react-hook-form';
import { type VariantProps, cva } from 'class-variance-authority';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

const formInputVariants = cva(
    'w-full rounded-lg border bg-background text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2',
    {
        variants: {
            variant: {
                default: 'border-input px-3 py-2 focus:ring-ring',
                auth: 'border-border px-3.5 py-2.5 focus:border-primary focus:ring-primary/10',
                muted: 'border-transparent bg-muted/30 px-3 py-2.5 hover:border-primary/50 focus:border-primary focus:bg-background focus:ring-primary',
            },
            hasError: {
                true: 'border-destructive focus:ring-destructive/10',
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
    label: string;
    error?: string | FieldError;
    containerClassName?: string;
}

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-001: React Hook Form & Lightweight Wrappers
 */
export const FormInput = forwardRef<HTMLInputElement, FormInputProps>(
    ({ label, error, variant, className, containerClassName, id, ...props }, ref) => {
        const errorMessages = getFieldErrors(error);
        const hasError = errorMessages.length > 0;

        return (
            <div className={cn('space-y-1', containerClassName)}>
                <label htmlFor={id} className="text-sm font-medium text-foreground">
                    {label}
                </label>
                <input
                    id={id}
                    ref={ref}
                    className={cn(formInputVariants({ variant, hasError, className }))}
                    {...props}
                />
                {errorMessages.length > 0 && (
                    <div className="mt-1 space-y-1">
                        {errorMessages.map((msg, idx) => (
                            <p key={idx} className="text-sm text-destructive">
                                {msg}
                            </p>
                        ))}
                    </div>
                )}
            </div>
        );
    },
);

FormInput.displayName = 'FormInput';
