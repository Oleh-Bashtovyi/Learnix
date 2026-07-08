import React, { forwardRef } from 'react';
import type { FieldError } from 'react-hook-form';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

interface FormCheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
    label: React.ReactNode;
    error?: string | FieldError;
    containerClassName?: string;
    labelClassName?: string;
}

export const FormCheckbox = forwardRef<HTMLInputElement, FormCheckboxProps>(
    ({ label, error, className, containerClassName, labelClassName, id, ...props }, ref) => {
        const errorMessages = getFieldErrors(error);

        return (
            <div className={cn('space-y-1', containerClassName)}>
                <label
                    htmlFor={id}
                    className={cn(
                        'flex cursor-pointer items-center gap-2 text-sm text-foreground',
                        labelClassName,
                    )}
                >
                    <input
                        type="checkbox"
                        id={id}
                        ref={ref}
                        className={cn(
                            'size-4 cursor-pointer rounded border-border text-primary accent-primary outline-none focus:ring-2 focus:ring-primary/50 focus:ring-offset-1 focus:ring-offset-background',
                            errorMessages.length > 0 && 'border-destructive',
                            className,
                        )}
                        {...props}
                    />
                    {label}
                </label>
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

FormCheckbox.displayName = 'FormCheckbox';
