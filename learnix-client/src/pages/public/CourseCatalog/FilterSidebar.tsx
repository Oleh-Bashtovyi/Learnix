import { useTranslation } from 'react-i18next';
import { RatingStars } from '@/components/common/elements/RatingStars';
import { RadioOption } from './RadioOption';

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
        { label: t('common:general.free'), value: true },
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
                    {t('common:general.category')}
                </h3>
                <div className="max-h-[280px] space-y-1 overflow-y-auto overscroll-contain pr-3 text-sm">
                    {categories.map((cat) => {
                        const checked = cat.id === selectedCategoryId;
                        return (
                            <RadioOption
                                key={cat.id}
                                name="category"
                                checked={checked}
                                onClick={() => onCategoryChange(checked ? '' : cat.id)}
                                label={cat.name}
                                rightElement={
                                    <span className="text-sm text-muted-foreground/80">
                                        {cat.coursesCount}
                                    </span>
                                }
                            />
                        );
                    })}
                </div>
            </div>

            {/* Price */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-4 font-heading font-semibold text-foreground/90">
                    {t('common:general.price')}
                </h3>
                <div className="space-y-1 pr-1 text-sm">
                    {PRICE_OPTIONS.map((opt) => {
                        const checked = isFree === opt.value;
                        return (
                            <RadioOption
                                key={String(opt.value)}
                                name="price"
                                checked={checked}
                                onClick={() => onPriceChange(checked ? undefined : opt.value)}
                                label={opt.label}
                            />
                        );
                    })}
                </div>
            </div>

            {/* Rating */}
            <div className="rounded-xl border border-border bg-card p-5">
                <h3 className="mb-4 font-heading font-semibold text-foreground/90">
                    {t('filters.rating')}
                </h3>
                <div className="space-y-1 pr-1 text-sm">
                    {RATING_OPTIONS.map((opt) => {
                        const checked = minRating === opt.value;
                        const labelContent =
                            opt.value === undefined ? (
                                <span>{opt.label}</span>
                            ) : (
                                <div className="flex items-center gap-1.5">
                                    <RatingStars value={opt.value} size="sm" />
                                    <span>{opt.value}+</span>
                                </div>
                            );

                        return (
                            <RadioOption
                                key={String(opt.value)}
                                name="rating"
                                checked={checked}
                                onClick={() => onRatingChange(checked ? undefined : opt.value)}
                                label={labelContent}
                            />
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
