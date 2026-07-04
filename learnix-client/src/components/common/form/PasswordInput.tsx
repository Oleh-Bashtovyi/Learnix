import React, { forwardRef, useState } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { cn } from '@/utils/cn';

interface PasswordInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
    label: string;
    error?: string;
    containerClassName?: string;
}

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
    ({ label, error, className, containerClassName, id, ...props }, ref) => {
        const [showPassword, setShowPassword] = useState(false);

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
                        className={cn(
                            'w-full rounded-lg border bg-background py-2 pl-3 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
                            'focus:ring-2 focus:ring-ring',
                            error ? 'border-destructive focus:ring-destructive/10' : 'border-input',
                            className,
                        )}
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
                {error && <p className="text-sm text-destructive">{error}</p>}
            </div>
        );
    },
);

PasswordInput.displayName = 'PasswordInput';
