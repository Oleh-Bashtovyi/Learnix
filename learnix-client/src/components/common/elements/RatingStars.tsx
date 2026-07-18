import { useState } from 'react';
import { Star, StarHalf } from 'lucide-react';
import { cn } from '@/utils/cn';

interface RatingStarsProps {
    value: number;
    max?: number;
    onChange?: (value: number) => void;
    size?: 'sm' | 'md' | 'lg' | 'xl';
    className?: string;
}

// xl exists for the one place where picking the rating *is* the task rather than a detail beside it.
const SIZE_MAP = {
    sm: 'h-3 w-3',
    md: 'h-5 w-5',
    lg: 'h-7 w-7',
    xl: 'h-9 w-9',
};

export function RatingStars({
    value,
    max = 5,
    onChange,
    size = 'md',
    className,
}: RatingStarsProps) {
    const isInteractive = !!onChange;
    const [hoverValue, setHoverValue] = useState<number | null>(null);

    return (
        <div
            className={cn('flex items-center gap-0.5', className)}
            onMouseLeave={() => setHoverValue(null)}
        >
            {Array.from({ length: max }, (_, i) => {
                const starValue = i + 1;
                const displayValue = isInteractive && hoverValue !== null ? hoverValue : value;

                const isFull = displayValue >= starValue;
                const isHalf = !isFull && displayValue > i;

                const content = (
                    <div className="relative">
                        <Star
                            className={cn(
                                SIZE_MAP[size],
                                'transition-colors duration-200',
                                isFull
                                    ? 'fill-warning text-warning'
                                    : 'fill-muted text-muted-foreground/30',
                            )}
                        />
                        {isHalf && (
                            <StarHalf
                                className={cn(
                                    SIZE_MAP[size],
                                    'absolute left-0 top-0 fill-warning text-warning',
                                )}
                            />
                        )}
                    </div>
                );

                if (isInteractive) {
                    return (
                        <button
                            key={i}
                            type="button"
                            onClick={() => onChange?.(starValue)}
                            onMouseEnter={() => setHoverValue(starValue)}
                            className="cursor-pointer transition-transform hover:scale-110"
                            aria-label={`${starValue} star${starValue !== 1 ? 's' : ''}`}
                        >
                            {content}
                        </button>
                    );
                }

                return (
                    <span
                        key={i}
                        className="cursor-default"
                        aria-label={`${starValue} star${starValue !== 1 ? 's' : ''}`}
                    >
                        {content}
                    </span>
                );
            })}
        </div>
    );
}
