import { useTranslation } from 'react-i18next';
import { ChevronDown } from 'lucide-react';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

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

    const selectedLabel = OPTIONS.find((o) => o.value === value)?.label;

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <button
                    type="button"
                    className="flex w-[200px] items-center justify-between rounded-xl border border-border bg-card px-4 py-2.5 text-sm font-medium shadow-sm transition-colors hover:bg-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                >
                    <span className="truncate">{selectedLabel}</span>
                    <ChevronDown className="ml-2 size-4 shrink-0 opacity-50" />
                </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-[200px] rounded-xl">
                {OPTIONS.map((opt) => (
                    <DropdownMenuItem
                        key={opt.value}
                        className="cursor-pointer py-2 text-sm"
                        onClick={() => onChange(opt.value)}
                    >
                        {opt.label}
                    </DropdownMenuItem>
                ))}
            </DropdownMenuContent>
        </DropdownMenu>
    );
}
