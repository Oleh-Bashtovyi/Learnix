import { FormSelect } from '@/components/common/form/FormSelect';

interface PageSizeSelectProps {
    value: number;
    onChange: (size: number) => void;
    options: readonly number[];
    label?: string;
    /** Surface the control sits on. Defaults to `default` (page background). */
    variant?: 'default' | 'card';
    className?: string;
}

/**
 * The shared "items per page" control, built on FormSelect so it matches every other field/select.
 * Used by the course catalog and the admin tables.
 */
export function PageSizeSelect({
    value,
    onChange,
    options,
    label,
    variant = 'default',
    className,
}: PageSizeSelectProps) {
    return (
        <div className="flex items-center gap-2">
            {label && <span className="text-sm text-muted-foreground">{label}</span>}
            <FormSelect
                value={String(value)}
                onValueChange={(v) => onChange(Number(v))}
                variant={variant}
                options={options.map((size) => ({ value: String(size), label: String(size) }))}
                triggerClassName={className ?? 'w-[72px]'}
            />
        </div>
    );
}
