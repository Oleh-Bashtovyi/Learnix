import type { ReactNode } from 'react';
import { cn } from '@/utils/cn';

/**
 * Colour here is a signal, not decoration. A row of tiles should carry at most one coloured tile —
 * the figure that actually has to be seen first — and leave the rest `neutral`; `success` and
 * `destructive` are reserved for tiles that genuinely mean succeeded / failed.
 *
 * Every row used to run brand → accent → success regardless of what the numbers were, which cost
 * twice over: the accent and success tints sit next to each other on the colour wheel and blurred
 * into one another, and the tile that mattered most (a test's passing threshold) was painted the
 * quietest of the three. A tile row is not a palette showcase.
 */
export type StatTone = 'neutral' | 'brand' | 'accent' | 'success' | 'warning' | 'destructive';

/**
 * Which surface the tile fills against, mirroring the form-field `default`/`card` idea. `panel` is the
 * subtle fill for a tile sitting inside a HeroPanel (the default — leaves existing usages unchanged);
 * `card` fills like a standalone card, for a tile placed directly on the page background where the
 * subtle fill would otherwise vanish into it.
 */
export type StatSurface = 'panel' | 'card';

/**
 * Movement over a recent window, shown beside the figure. The figure itself is a total, so the
 * trend states its own terms: `delta` is what the window added, `changePercent` how that compares
 * with the window before it, and `title` says which window both are about.
 */
export interface StatTrend {
    /** Signed change against the previous window; null when there is nothing to compare against. */
    changePercent: number | null;
    /** Already-formatted amount the window added, e.g. `+$1,200`. Omitted when it added nothing. */
    delta?: string;
    title?: string;
}

interface StatTileProps {
    icon: ReactNode;
    tone: StatTone;
    label: string;
    /** A node, not a string: some values are not text — an unlimited count is an icon, not a glyph. */
    value: ReactNode;
    /** A small caption beside the value — the count an average rests on, a total to measure against. */
    hint?: string;
    trend?: StatTrend;
    surface?: StatSurface;
    className?: string;
}

const SURFACE_CLASSES: Record<StatSurface, string> = {
    panel: 'bg-background/40',
    card: 'bg-card',
};

const TONE_CLASSES: Record<StatTone, { chip: string; hover: string }> = {
    neutral: {
        chip: 'border-border bg-gradient-to-br from-muted-foreground/15 to-muted-foreground/5 text-muted-foreground',
        hover: 'hover:border-muted-foreground/40',
    },
    brand: {
        chip: 'border-brand/20 bg-gradient-to-br from-brand/20 to-brand/5 text-brand',
        hover: 'hover:border-brand/40',
    },
    accent: {
        chip: 'border-accent/20 bg-gradient-to-br from-accent/20 to-accent/5 text-accent-strong',
        hover: 'hover:border-accent/40',
    },
    success: {
        chip: 'border-success/20 bg-gradient-to-br from-success/20 to-success/5 text-success',
        hover: 'hover:border-success/40',
    },
    warning: {
        chip: 'border-warning/20 bg-gradient-to-br from-warning/20 to-warning/5 text-warning',
        hover: 'hover:border-warning/40',
    },
    destructive: {
        chip: 'border-destructive/20 bg-gradient-to-br from-destructive/20 to-destructive/5 text-destructive',
        hover: 'hover:border-destructive/40',
    },
};

/** One figure inside a HeroPanel: a tinted icon chip, the number, and what it counts. */
export function StatTile({
    icon,
    tone,
    label,
    value,
    hint,
    trend,
    surface = 'panel',
    className,
}: StatTileProps) {
    const tones = TONE_CLASSES[tone];
    const isUp = (trend?.changePercent ?? 0) >= 0;

    return (
        <div
            className={cn(
                'group flex items-center gap-3 rounded-xl border border-border p-3.5 transition-colors',
                SURFACE_CLASSES[surface],
                tones.hover,
                className,
            )}
        >
            <div
                className={cn(
                    'grid size-11 shrink-0 place-items-center rounded-lg border transition-transform group-hover:scale-105',
                    tones.chip,
                )}
            >
                {icon}
            </div>
            <div className="min-w-0">
                {/* The label is a single line — a wrapped caption above the figure reads as two titles. */}
                <dt className="truncate text-xs text-muted-foreground">{label}</dt>
                {/* Label + this row are the two lines a square icon is exactly tall enough for. The hint
                    rides the figure's baseline as a suffix rather than becoming an ill-fitting third row. */}
                <dd className="flex items-baseline gap-2.5">
                    <span className="font-heading text-lg font-semibold leading-tight text-foreground">
                        {value}
                    </span>
                    {hint && (
                        <span className="truncate text-[11px] text-muted-foreground/70">
                            {hint}
                        </span>
                    )}
                    {trend && (trend.changePercent !== null || trend.delta) && (
                        <span
                            title={trend.title}
                            className="flex shrink-0 items-baseline gap-1 text-[11px]"
                        >
                            {trend.changePercent !== null && (
                                <span
                                    className={cn(
                                        'flex items-baseline gap-0.5 font-medium',
                                        isUp ? 'text-success' : 'text-destructive',
                                    )}
                                >
                                    {isUp ? '↑' : '↓'}
                                    {Math.abs(trend.changePercent)}%
                                </span>
                            )}
                            {trend.delta && (
                                <span className="text-muted-foreground/70">{trend.delta}</span>
                            )}
                        </span>
                    )}
                </dd>
            </div>
        </div>
    );
}
