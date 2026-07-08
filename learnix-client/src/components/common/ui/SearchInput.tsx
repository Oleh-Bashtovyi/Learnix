import React, { forwardRef } from 'react';
import { type VariantProps, cva } from 'class-variance-authority';
import { Search, X } from 'lucide-react';
import { cn } from '@/utils/cn';

const searchInputVariants = cva(
    'w-full rounded-lg border bg-background pl-9 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2',
    {
        variants: {
            variant: {
                default: 'border-input py-2 pr-10 focus:ring-ring',
                muted: 'border-transparent bg-muted/30 py-2 pr-10 hover:border-primary/50 focus:border-primary focus:bg-background focus:ring-primary/20',
            },
        },
        defaultVariants: {
            variant: 'default',
        },
    },
);

export interface SearchInputProps
    extends
        Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'>,
        VariantProps<typeof searchInputVariants> {
    onClear?: () => void;
    containerClassName?: string;
}

export const SearchInput = forwardRef<HTMLInputElement, SearchInputProps>(
    ({ className, containerClassName, variant, onClear, value, ...props }, ref) => {
        return (
            <div className={cn('relative flex items-center', containerClassName)}>
                <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <input
                    ref={ref}
                    type="text"
                    value={value}
                    className={cn(searchInputVariants({ variant, className }))}
                    {...props}
                />
                {value && onClear && (
                    <button
                        type="button"
                        onClick={onClear}
                        className="absolute right-3 top-1/2 -translate-y-1/2 rounded-md p-1 text-muted-foreground hover:bg-muted hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                        aria-label="Clear search"
                    >
                        <X size={14} />
                    </button>
                )}
            </div>
        );
    },
);

SearchInput.displayName = 'SearchInput';
