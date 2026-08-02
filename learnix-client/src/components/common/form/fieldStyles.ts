/**
 * Single source of truth for form-control styling.
 *
 * Every text-like control (Input, FormInput, FormTextarea, PasswordInput, SearchInput, FormSelect)
 * and the choice controls (FormCheckbox, RadioOption) build on these class strings, which resolve
 * to the `--field-*` design tokens in `styles/index.css`. To restyle or rebrand all form controls,
 * edit the tokens (or these strings) — never reach for --primary/--brand or per-page className
 * overrides that re-declare border/fill/focus.
 *
 * Surfaces: FIELD_BASE fills with the `default` surface (for controls on the page background).
 * A control inside a card should add FIELD_SURFACE_CARD, which swaps the fill so the input reads
 * against the card behind it. This is exposed as the `variant: 'default' | 'card'` prop.
 *
 * Related ADR: ADR-FRONT-UI-006 (docs/frontend/decisions/platform/UI.md).
 */

/** Border, default fill, focus ring and disabled treatment shared by every text field. Add padding per component. */
export const FIELD_BASE =
    'w-full rounded-lg border border-field-border bg-field text-sm text-foreground shadow-sm outline-none transition-colors placeholder:text-muted-foreground hover:bg-field-hover focus:border-field-focus focus:ring-2 focus:ring-field-focus/20 disabled:cursor-not-allowed disabled:opacity-50';

/** Swaps the default fill for the in-card fill. Appended by the `card` variant. */
export const FIELD_SURFACE_CARD = 'bg-field-card hover:bg-field-card-hover';

/** Error state — overrides the resting/focus border so the field reads as invalid. */
export const FIELD_ERROR =
    'border-field-error hover:border-field-error focus:border-field-error focus:ring-field-error/20';

/** Checkbox / radio accent + focus ring, shared by FormCheckbox and RadioOption. */
export const FIELD_ACCENT = 'accent-field-accent focus-visible:ring-field-focus';
