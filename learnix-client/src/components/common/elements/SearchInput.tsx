import React, { forwardRef } from 'react';
import { type VariantProps, cva } from 'class-variance-authority';
import { Search, X } from 'lucide-react';
import { FIELD_BASE, FIELD_SURFACE_CARD } from '@/components/common/form/fieldStyles';
import { cn } from '@/utils/cn';

// Border/focus/fill come from the shared field tokens; the only choice is the surface.
// py-2.5 keeps search bars the same height as every other field (FormInput/FormSelect);
// pl-9 clears the search icon, pr-10 the clear button.
const searchInputVariants = cva(`${FIELD_BASE} py-2.5 pl-9 pr-10`, {
    variants: {
        variant: {
            default: '',
            card: FIELD_SURFACE_CARD,
        },
    },
    defaultVariants: {
        variant: 'default',
    },
});

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
                        className="absolute right-3 top-1/2 -translate-y-1/2 rounded-md p-1 text-muted-foreground hover:bg-muted hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-field-focus"
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
