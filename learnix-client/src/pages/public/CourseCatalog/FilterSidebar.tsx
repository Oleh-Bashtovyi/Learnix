import { type ReactNode } from 'react';
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

interface CustomRadioProps {
    name: string;
    checked: boolean;
    onClick: () => void;
    label: ReactNode;
    rightElement?: ReactNode;
}

function CustomRadio({ name, checked, onClick, label, rightElement }: CustomRadioProps) {
    return (
        <label className="group flex cursor-pointer items-center justify-between py-1.5 transition-all">
            <div className="flex items-center gap-3">
                <div
                    className={cn(
                        'relative flex size-5 shrink-0 items-center justify-center rounded-full border-2 transition-colors',
                        checked
                            ? 'border-primary bg-primary/10'
                            : 'border-muted-foreground/30 bg-transparent group-hover:border-primary/50',
                    )}
                >
                    <input
                        type="radio"
                        name={name}
                        checked={checked}
                        onClick={onClick}
                        readOnly
                        className="absolute inset-0 cursor-pointer opacity-0"
                    />
                    <div
                        className={cn(
                            'size-2.5 rounded-full transition-all duration-200',
                            checked ? 'scale-100 bg-primary' : 'scale-0 bg-transparent',
                        )}
                    />
                </div>
                <span
                    className={cn(
                        'text-sm transition-colors',
                        checked
                            ? 'font-medium text-primary'
                            : 'text-foreground/80 group-hover:text-foreground',
                    )}
                >
                    {label}
                </span>
            </div>
            {rightElement}
        </label>
    );
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
                        const checked = cat.id === selectedCategoryId;
                        return (
                            <CustomRadio
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
                    {t('filters.price')}
                </h3>
                <div className="space-y-1 pr-1 text-sm">
                    {PRICE_OPTIONS.map((opt) => {
                        const checked = isFree === opt.value;
                        return (
                            <CustomRadio
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
                            <CustomRadio
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
