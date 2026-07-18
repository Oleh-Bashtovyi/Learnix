import { cn } from '@/utils/cn';

interface CountBadgeProps {
    count: number;
    /**
     * `corner` overlays an icon button and needs a `relative` parent; `inline` sits in the
     * flow, pushed to the end of a row.
     */
    placement?: 'corner' | 'inline';
}

const PLACEMENT_CLASSES: Record<NonNullable<CountBadgeProps['placement']>, string> = {
    // Offset outwards: a badge centred on a glyph hides the very icon it annotates.
    corner: 'absolute -right-0.5 -top-0.5',
    inline: 'ml-auto',
};

/** The unread/saved counter, on a header icon button or in a sidebar nav row. */
export function CountBadge({ count, placement = 'corner' }: CountBadgeProps) {
    if (count <= 0) return null;

    return (
        <span
            className={cn(
                'flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1',
                'text-[10px] font-bold leading-none text-destructive-foreground',
                PLACEMENT_CLASSES[placement],
            )}
        >
            {count > 99 ? '99+' : count}
        </span>
    );
}
