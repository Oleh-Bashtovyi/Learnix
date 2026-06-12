import { useTranslation } from 'react-i18next';

type SortBy = 'popular' | 'newest' | 'rating';

interface SortDropdownProps {
    value: SortBy;
    onChange: (value: SortBy) => void;
}

export function SortDropdown({ value, onChange }: SortDropdownProps) {
    const { t } = useTranslation('catalog');

    const OPTIONS: { value: SortBy; label: string }[] = [
        { value: 'popular', label: t('sort.popular') },
        { value: 'newest', label: t('sort.newest') },
        { value: 'rating', label: t('sort.rating') },
    ];

    return (
        <div className="relative shrink-0">
            <select
                value={value}
                onChange={(e) => onChange(e.target.value as SortBy)}
                className="cursor-pointer appearance-none rounded-xl border border-border bg-card px-4 py-2.5 pr-10 text-sm font-medium shadow-sm transition-colors hover:bg-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                aria-label={t('sort.label')}
            >
                {OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value} className="bg-card text-foreground">
                        {opt.label}
                    </option>
                ))}
            </select>
            <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3 text-muted-foreground">
                <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M19 9l-7 7-7-7"
                    />
                </svg>
            </div>
        </div>
    );
}
