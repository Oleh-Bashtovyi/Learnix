import { Toaster } from 'sonner';
import { useThemeStore } from '@/store/theme.store';

/**
 * Sonner keeps its own light/dark state and defaults to light, so without this it would paint
 * light toasts over a dark page. The colours themselves come from the design tokens — see the
 * `[data-sonner-toaster]` block in `styles/index.css`.
 *
 * `expand` is on because the platform can genuinely fire two toasts at once — finishing a course
 * issues a certificate and unlocks an achievement in the same instant. Sonner's default is to
 * collapse a stack into a deck, showing only the front toast with the others peeking out behind it,
 * and to fan them out on hover. That deck is built for toasts of one shape; ours are not, and two
 * differently sized cards laid over each other read as a rendering bug rather than as a stack. They
 * are stacked openly instead — the same thing the deck expands into on hover, minus the hovering.
 */
export function AppToaster() {
    const theme = useThemeStore((s) => s.theme);

    return <Toaster position="top-right" offset="80px" expand richColors theme={theme} />;
}
