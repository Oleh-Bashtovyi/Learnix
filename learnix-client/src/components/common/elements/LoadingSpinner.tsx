import { Loader2 } from 'lucide-react';
import { cn } from '@/utils/cn';

const SPINNER_SIZES = {
    sm: 'size-4',
    md: 'size-6',
    lg: 'size-10',
} as const;

interface LoadingSpinnerProps {
    size?: keyof typeof SPINNER_SIZES;
    className?: string;
}

export function LoadingSpinner({ size = 'md', className }: LoadingSpinnerProps) {
    return (
        <div className={cn('flex items-center justify-center p-8', className)}>
            <Loader2 className={cn('animate-spin text-primary', SPINNER_SIZES[size])} />
        </div>
    );
}
