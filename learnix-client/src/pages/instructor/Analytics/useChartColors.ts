import { useMemo } from 'react';
import { useThemeStore } from '@/store/theme.store';

/**
 * Resolves the design-token HSL values to concrete color strings for Recharts. Recharts writes colors
 * as SVG presentation attributes, where `var(--token)` does not reliably resolve — so we read the
 * computed values instead, and re-read whenever the theme flips so dark mode gets its own palette.
 */
export function useChartColors() {
    const theme = useThemeStore((s) => s.theme);

    return useMemo(() => {
        const style = getComputedStyle(document.documentElement);
        const token = (name: string) => `hsl(${style.getPropertyValue(name).trim()})`;

        return {
            primary: token('--primary'),
            accent: token('--accent'),
            success: token('--success'),
            warning: token('--warning'),
            muted: token('--muted-foreground'),
            border: token('--border'),
            // Depends on `theme` so the memo re-reads the (now different) computed values on toggle.
            _theme: theme,
        };
    }, [theme]);
}

export type ChartColors = ReturnType<typeof useChartColors>;
