import { AlertCircle } from 'lucide-react';
import { cn } from '@/utils/cn';

interface QueryErrorProps {
    message: string;
    onRetry?: () => void;
    retryLabel?: string;
    className?: string;
}

export function QueryError({ message, onRetry, retryLabel, className }: QueryErrorProps) {
    return (
        <div
            className={cn(
                'flex min-h-[200px] flex-col items-center justify-center gap-3 text-center',
                className,
            )}
        >
            <AlertCircle className="size-10 text-destructive/60" />
            <p className="text-sm text-muted-foreground">{message}</p>
            {onRetry && retryLabel && (
                <button
                    type="button"
                    onClick={onRetry}
                    className="text-sm text-primary underline hover:text-primary/80"
                >
                    {retryLabel}
                </button>
            )}
        </div>
    );
}
