import type { ReactNode } from 'react';

interface TooltipRow {
    label: string;
    value: ReactNode;
    color?: string;
}

interface ChartTooltipProps {
    active?: boolean;
    title?: ReactNode;
    rows: TooltipRow[];
}

/** Themed replacement for Recharts' default (white) tooltip — reads the app's surface/ink tokens. */
export function ChartTooltip({ active, title, rows }: ChartTooltipProps) {
    if (!active || rows.length === 0) return null;

    return (
        <div className="rounded-lg border border-border bg-popover px-3 py-2 shadow-lg">
            {title != null && <p className="mb-1 text-xs font-medium text-foreground">{title}</p>}
            <ul className="space-y-0.5">
                {rows.map((row, i) => (
                    <li key={i} className="flex items-center gap-2 text-xs">
                        {row.color && (
                            <span
                                className="size-2 shrink-0 rounded-full"
                                style={{ backgroundColor: row.color }}
                            />
                        )}
                        <span className="text-muted-foreground">{row.label}</span>
                        <span className="ml-auto font-medium text-foreground">{row.value}</span>
                    </li>
                ))}
            </ul>
        </div>
    );
}
