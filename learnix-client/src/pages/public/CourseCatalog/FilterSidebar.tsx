import { useTranslation } from 'react-i18next';
import { RatingStars } from '@/components/common/ui/RatingStars';
import { cn } from '@/utils/cn';

interface CategoryOption {
    id: string;
    name: string;
    coursesCount: number;
}

interface FilterSidebarProps {
    categories: CategoryOption[];
    selectedCategoryId: string;
    isFree: boolean | undefined;
    minRating: number | undefined;
    onCategoryChange: (id: string) => void;
    onPriceChange: (val: boolean | undefined) => void;
    onRatingChange: (val: number | undefined) => void;
    onClear: () => void;
}

export function FilterSidebar({
    categories,
    selectedCategoryId,
    isFree,
    minRating,
    onCategoryChange,
    onPriceChange,
    onRatingChange,
    onClear,
}: FilterSidebarProps) {
    const { t } = useTranslation('catalog');

    const PRICE_OPTIONS: { label: string; value: boolean | undefined }[] = [
        { label: t('filters.priceAll'), value: undefined },
        { label: t('filters.priceFree'), value: true },
        { label: t('filters.pricePaid'), value: false },
    ];

    const RATING_OPTIONS: { label: string; value: number | undefined }[] = [
        { label: t('filters.ratingAll'), value: undefined },
        { label: t('filters.rating45'), value: 4.5 },
        { label: t('filters.rating40'), value: 4.0 },
        { label: t('filters.rating35'), value: 3.5 },
    ];

    const hasActiveFilters =
        !!selectedCategoryId || isFree !== undefined || minRating !== undefined;

    return (
        <aside className="space-y-4">
            {/* Category */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-4 font-heading font-semibold text-foreground/90">
                    {t('filters.category')}
                </h3>
                <div className="max-h-[280px] space-y-1 overflow-y-auto overscroll-contain pr-3 text-sm">
                    {categories.map((cat) => {
                        const selected = cat.id === selectedCategoryId;
                        return (
                            <button
                                key={cat.id}
                                type="button"
                                onClick={() => onCategoryChange(selected ? '' : cat.id)}
                                className={cn(
                                    'flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left transition-colors',
                                    selected
                                        ? 'bg-primary/10 text-primary'
                                        : 'hover:bg-muted hover:text-foreground',
                                )}
                            >
                                <span className={cn(selected && 'font-medium')}>{cat.name}</span>
                                <span className="text-muted-foreground">{cat.coursesCount}</span>
                            </button>
                        );
                    })}
                </div>
            </div>

            {/* Price */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-4 font-heading font-semibold text-foreground/90">
                    {t('filters.price')}
                </h3>
                <div className="space-y-2 pr-1 text-sm">
                    {PRICE_OPTIONS.map((opt) => {
                        const checked = isFree === opt.value;
                        return (
                            <label
                                key={String(opt.value)}
                                className="flex cursor-pointer items-center gap-2 hover:text-primary"
                            >
                                <input
                                    type="radio"
                                    name="price"
                                    checked={checked}
                                    onClick={() => onPriceChange(checked ? undefined : opt.value)}
                                    readOnly
                                    className="accent-primary"
                                />
                                {opt.label}
                            </label>
                        );
                    })}
                </div>
            </div>

            {/* Rating */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-4 font-heading font-semibold text-foreground/90">
                    {t('filters.rating')}
                </h3>
                <div className="space-y-2 pr-1 text-sm">
                    {RATING_OPTIONS.map((opt) => {
                        const checked = minRating === opt.value;
                        return (
                            <label
                                key={String(opt.value)}
                                className="flex cursor-pointer items-center gap-2 hover:text-primary"
                            >
                                <input
                                    type="radio"
                                    name="rating"
                                    checked={checked}
                                    onClick={() => onRatingChange(checked ? undefined : opt.value)}
                                    readOnly
                                    className="accent-primary"
                                />
                                {opt.value === undefined ? (
                                    <span>{opt.label}</span>
                                ) : (
                                    <>
                                        <RatingStars value={opt.value} size="sm" />
                                        <span>{opt.value}+</span>
                                    </>
                                )}
                            </label>
                        );
                    })}
                </div>
            </div>

            {hasActiveFilters && (
                <button
                    type="button"
                    onClick={onClear}
                    className="w-full text-sm text-muted-foreground transition-colors hover:text-destructive"
                >
                    {t('clearFilters')}
                </button>
            )}
        </aside>
    );
}
