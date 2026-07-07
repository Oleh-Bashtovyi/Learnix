import React, { forwardRef, useState } from 'react';
import type { FieldError } from 'react-hook-form';
import { type VariantProps, cva } from 'class-variance-authority';
import { Eye, EyeOff } from 'lucide-react';
import { cn } from '@/utils/cn';
import { getFieldErrors } from '@/utils/errors';

const passwordInputVariants = cva(
    'w-full rounded-lg border bg-background pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2',
    {
        variants: {
            variant: {
                default: 'border-input py-2 pl-3 focus:ring-ring',
                auth: 'border-border py-2.5 pl-3.5 focus:border-primary focus:ring-primary/10',
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

interface PasswordInputProps
    extends
        Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'>,
        VariantProps<typeof passwordInputVariants> {
    label: string;
    error?: string | FieldError;
    containerClassName?: string;
}

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
    ({ label, error, variant, className, containerClassName, id, ...props }, ref) => {
        const [showPassword, setShowPassword] = useState(false);
        const errorMessages = getFieldErrors(error);
        const hasError = errorMessages.length > 0;

        return (
            <div className={cn('space-y-1.5', containerClassName)}>
                <label htmlFor={id} className="text-sm font-medium text-foreground">
                    {label}
                </label>
                <div className="relative">
                    <input
                        id={id}
                        ref={ref}
                        type={showPassword ? 'text' : 'password'}
                        className={cn(passwordInputVariants({ variant, hasError, className }))}
                        {...props}
                    />
                    <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground focus:outline-none"
                    >
                        {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </button>
                </div>
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

PasswordInput.displayName = 'PasswordInput';
