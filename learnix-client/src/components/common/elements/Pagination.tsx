import { type ReactNode, useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { cn } from '@/utils/cn';

interface PaginationProps {
    page: number;
    totalPages: number;
    onChange: (p: number) => void;
    prevLabel?: ReactNode;
    nextLabel?: ReactNode;
    /** Show a small "go to page" number input (desktop only). Useful when there are many pages. */
    showGoToPage?: boolean;
    goToLabel?: string;
    /** Compact "{page} / {total}" label used in the mobile layout. */
    pageOfLabel?: (page: number, total: number) => string;
    className?: string;
}

const edgeButton =
    'flex size-9 items-center justify-center rounded-lg border border-border text-sm transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40';

export function Pagination({
    page,
    totalPages,
    onChange,
    prevLabel,
    nextLabel,
    showGoToPage,
    goToLabel,
    pageOfLabel,
    className,
}: PaginationProps) {
    const [goToValue, setGoToValue] = useState('');

    if (totalPages <= 1) return null;

    const pages: (number | '...')[] = [];
    if (totalPages <= 7) {
        for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
        pages.push(1);
        if (page > 3) pages.push('...');
        for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++) {
            pages.push(i);
        }
        if (page < totalPages - 2) pages.push('...');
        pages.push(totalPages);
    }

    const goTo = () => {
        const n = Number.parseInt(goToValue, 10);
        setGoToValue('');
        if (!Number.isNaN(n)) onChange(Math.min(totalPages, Math.max(1, n)));
    };

    const prevButton = (
        <button
            type="button"
            onClick={() => onChange(page - 1)}
            disabled={page === 1}
            className={edgeButton}
        >
            {prevLabel || <ChevronLeft className="size-4" />}
        </button>
    );

    const nextButton = (
        <button
            type="button"
            onClick={() => onChange(page + 1)}
            disabled={page === totalPages}
            className={edgeButton}
        >
            {nextLabel || <ChevronRight className="size-4" />}
        </button>
    );

    return (
        <div className={cn('flex items-center justify-center gap-2', className)}>
            {/* Mobile: compact prev · "x / y" · next */}
            <div className="flex items-center gap-3 sm:hidden">
                {prevButton}
                <span className="text-sm font-medium tabular-nums text-muted-foreground">
                    {pageOfLabel ? pageOfLabel(page, totalPages) : `${page} / ${totalPages}`}
                </span>
                {nextButton}
            </div>

            {/* Desktop: full windowed numbers */}
            <div className="hidden items-center gap-1 sm:flex">
                {prevButton}
                {pages.map((p, idx) =>
                    p === '...' ? (
                        <span
                            key={`ellipsis-${idx}`}
                            className="grid size-9 place-items-center text-muted-foreground"
                        >
                            …
                        </span>
                    ) : (
                        <button
                            key={p}
                            type="button"
                            onClick={() => onChange(p as number)}
                            className={cn(
                                'grid size-9 place-items-center rounded-lg text-sm font-medium tabular-nums transition-colors',
                                p === page
                                    ? 'bg-primary text-primary-foreground'
                                    : 'border border-border hover:bg-secondary',
                            )}
                        >
                            {p}
                        </button>
                    ),
                )}
                {nextButton}
            </div>

            {/* Desktop-only: go to page */}
            {showGoToPage && (
                <div className="hidden items-center gap-2 pl-2 md:flex">
                    {goToLabel && (
                        <span className="text-sm text-muted-foreground">{goToLabel}</span>
                    )}
                    <Input
                        type="number"
                        inputMode="numeric"
                        min={1}
                        max={totalPages}
                        value={goToValue}
                        onChange={(e) => setGoToValue(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === 'Enter') goTo();
                        }}
                        onBlur={goTo}
                        aria-label={goToLabel}
                        className="h-9 w-16 px-2 text-center [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
                    />
                </div>
            )}
        </div>
    );
}
