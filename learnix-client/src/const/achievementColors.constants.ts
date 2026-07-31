/**
 * Gradient stops for achievement badges — each achievement's own colour, independent of the app
 * theme (used identically in light and dark). Not CSS custom properties: consumers interpolate
 * these into an inline `linear-gradient(...)` string, so there is nothing for a token to resolve.
 *
 * Several achievement codes share a family (gold, green, blue) on purpose — declared once here so
 * changing "the gold gradient" changes every achievement that uses it.
 */
export const ACHIEVEMENT_GRADIENTS = {
    gold: ['#fef08a', '#eab308'],
    teal: ['#99f6e4', '#14b8a6'],
    purple: ['#e9d5ff', '#a855f7'],
    fuchsia: ['#f5d0fe', '#d946ef'],
    green: ['#86efac', '#22c55e'],
    blue: ['#bfdbfe', '#3b82f6'],
    red: ['#fecaca', '#ef4444'],
} as const satisfies Record<string, [string, string]>;

export type AchievementGradient = keyof typeof ACHIEVEMENT_GRADIENTS;
