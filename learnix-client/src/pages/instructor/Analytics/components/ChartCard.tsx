import type { ReactNode, RefObject } from 'react';
import { useTranslation } from 'react-i18next';
import { LoadingSpinner } from '@/components/common/elements/LoadingSpinner';
import { QueryError } from '@/components/common/system/QueryError';
import { cn } from '@/utils/cn';

interface ChartCardProps {
    title: string;
    action?: ReactNode;
    isLoading?: boolean;
    isError?: boolean;
    isEmpty?: boolean;
    /** Overrides the generic "no data yet" copy when the empty state has something more specific to say. */
    emptyMessage?: string;
    onRetry?: () => void;
    className?: string;
    rootRef?: RefObject<HTMLDivElement | null>;
    children: ReactNode;
}

export function ChartCard({
    title,
    action,
    isLoading,
    isError,
    isEmpty,
    emptyMessage,
    onRetry,
    className,
    rootRef,
    children,
}: ChartCardProps) {
    const { t } = useTranslation('instructorAnalytics');

    return (
        <div
            ref={rootRef}
            className={cn('flex flex-col rounded-xl border border-border bg-card p-5', className)}
        >
            <div className="mb-4 flex items-center justify-between gap-3">
                <h3 className="font-heading text-sm font-semibold text-foreground">{title}</h3>
                {action}
            </div>

            {isLoading ? (
                <LoadingSpinner className="flex-1 py-10" />
            ) : isError ? (
                <QueryError
                    message={t('loadError')}
                    onRetry={onRetry}
                    retryLabel={t('common:actions.tryAgain')}
                    // A failed card must not hold the full chart height — it looks more broken than it is.
                    className="min-h-0 py-10"
                />
            ) : isEmpty ? (
                <p className="flex flex-1 items-center justify-center py-10 text-sm text-muted-foreground">
                    {emptyMessage ?? t('empty')}
                </p>
            ) : (
                children
            )}
        </div>
    );
}
