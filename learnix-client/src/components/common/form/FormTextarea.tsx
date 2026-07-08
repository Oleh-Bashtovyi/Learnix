import React, { forwardRef } from 'react';
import { type VariantProps, cva } from 'class-variance-authority';
import { cn } from '@/utils/cn';

const formTextareaVariants = cva(
    'w-full resize-none rounded-lg border bg-background text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2',
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

interface FormTextareaProps
    extends
        React.TextareaHTMLAttributes<HTMLTextAreaElement>,
        VariantProps<typeof formTextareaVariants> {
    label?: React.ReactNode;
    error?: string;
    containerClassName?: string;
}

export const FormTextarea = forwardRef<HTMLTextAreaElement, FormTextareaProps>(
    ({ label, error, variant, className, containerClassName, id, ...props }, ref) => {
        const hasError = !!error;
        return (
            <div className={cn('space-y-1', containerClassName)}>
                {label && (
                    <label htmlFor={id} className="text-sm font-medium text-foreground">
                        {label}
                    </label>
                )}
                <textarea
                    id={id}
                    ref={ref}
                    className={cn(formTextareaVariants({ variant, hasError, className }))}
                    {...props}
                />
                {error && <p className="text-sm text-destructive">{error}</p>}
            </div>
        );
    },
);

FormTextarea.displayName = 'FormTextarea';
