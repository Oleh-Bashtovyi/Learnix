import { cn } from '@/utils/cn';

interface PaginationProps {
    page: number;
    totalPages: number;
    onChange: (p: number) => void;
    prevLabel: string;
    nextLabel: string;
    className?: string;
}

export function Pagination({
    page,
    totalPages,
    onChange,
    prevLabel,
    nextLabel,
    className,
}: PaginationProps) {
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

    return (
        <div className={cn('flex justify-center gap-1.5', className)}>
            <button
                onClick={() => onChange(page - 1)}
                disabled={page === 1}
                className="flex h-9 items-center justify-center rounded-lg border border-border px-3 text-sm hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
            >
                {prevLabel}
            </button>
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
                        onClick={() => onChange(p as number)}
                        className={cn(
                            'grid h-9 w-9 place-items-center rounded-lg text-sm font-medium transition-colors',
                            p === page
                                ? 'bg-primary text-primary-foreground'
                                : 'border border-border hover:bg-secondary',
                        )}
                    >
                        {p}
                    </button>
                ),
            )}
            <button
                onClick={() => onChange(page + 1)}
                disabled={page === totalPages}
                className="flex h-9 items-center justify-center rounded-lg border border-border px-3 text-sm hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
            >
                {nextLabel}
            </button>
        </div>
    );
}
