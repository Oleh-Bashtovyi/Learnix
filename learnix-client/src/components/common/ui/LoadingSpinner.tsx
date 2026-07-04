import { cn } from '@/utils/cn';

interface LoadingSpinnerProps {
    className?: string;
}

export function LoadingSpinner({ className }: LoadingSpinnerProps) {
    return (
        <div className={cn('flex items-center justify-center p-8', className)}>
            <div className="size-6 animate-spin rounded-full border-2 border-border border-t-primary" />
        </div>
    );
}
