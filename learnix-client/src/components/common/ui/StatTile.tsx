import type { ReactNode } from 'react';
import { cn } from '@/utils/cn';

export type StatTone = 'brand' | 'accent' | 'success' | 'warning' | 'destructive';

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
    brand: {
        chip: 'border-brand/20 bg-gradient-to-br from-brand/20 to-brand/5 text-brand',
        hover: 'hover:border-brand/40',
    },
    accent: {
        chip: 'border-accent/20 bg-gradient-to-br from-accent/20 to-accent/5 text-accent',
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
