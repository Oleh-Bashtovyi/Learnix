import React, { forwardRef } from 'react';
import { cn } from '@/utils/cn';

interface FormInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
    label: string;
    error?: string;
    containerClassName?: string;
}

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-001: React Hook Form & Lightweight Wrappers
 */
export const FormInput = forwardRef<HTMLInputElement, FormInputProps>(
    ({ label, error, className, containerClassName, id, ...props }, ref) => {
        return (
            <div className={cn('space-y-1', containerClassName)}>
                <label htmlFor={id} className="text-sm font-medium text-foreground">
                    {label}
                </label>
                <input
                    id={id}
                    ref={ref}
                    className={cn(
                        'w-full rounded-lg border bg-background px-3 py-2 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
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

FormInput.displayName = 'FormInput';
