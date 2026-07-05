import React, { forwardRef } from 'react';
import { cn } from '@/utils/cn';

interface FormTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
    label: string;
    error?: string;
    containerClassName?: string;
}

export const FormTextarea = forwardRef<HTMLTextAreaElement, FormTextareaProps>(
    ({ label, error, className, containerClassName, id, ...props }, ref) => {
        return (
            <div className={cn('space-y-1', containerClassName)}>
                <label htmlFor={id} className="text-sm font-medium text-foreground">
                    {label}
                </label>
                <textarea
                    id={id}
                    ref={ref}
                    className={cn(
                        'w-full resize-none rounded-lg border bg-background px-3 py-2 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
                        'focus:ring-2 focus:ring-ring',
                        error ? 'border-destructive focus:ring-destructive/10' : 'border-input',
                        className,
                    )}
                    {...props}
                />
                {error && <p className="text-sm text-destructive">{error}</p>}
            </div>
        );
    },
);

FormTextarea.displayName = 'FormTextarea';
