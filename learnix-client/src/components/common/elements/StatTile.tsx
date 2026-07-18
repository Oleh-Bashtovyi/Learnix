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

interface StatTileProps {
    icon: ReactNode;
    tone: StatTone;
    label: string;
    /** A node, not a string: some values are not text — an unlimited count is an icon, not a glyph. */
    value: ReactNode;
    /** A second line under the value — the count an average rests on, a total to measure against. */
    hint?: string;
    className?: string;
}

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
export function StatTile({ icon, tone, label, value, hint, className }: StatTileProps) {
    const tones = TONE_CLASSES[tone];

    return (
        <div
            className={cn(
                'group flex items-center gap-3 rounded-xl border border-border bg-background/40 p-4 transition-colors',
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
                <dt className="text-xs text-muted-foreground">{label}</dt>
                <dd className="font-heading text-lg font-semibold leading-tight text-foreground">
                    {value}
                </dd>
                {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
            </div>
        </div>
    );
}
