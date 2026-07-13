import { useTranslation } from 'react-i18next';
import { Sparkles, X } from 'lucide-react';

interface AssistantHintProps {
    onDismiss: () => void;
}

/**
 * Shown once, ever, the first time somebody opens a lesson: the two buttons it points at are icons in
 * a crowded header, and nothing about them says the tutor and the instructor both already know which
 * lesson you are on. That is the part worth telling — the icons themselves are discoverable enough.
 *
 * It dismisses on any interaction with it, and never returns. A hint that comes back is a nag.
 */
export function AssistantHint({ onDismiss }: AssistantHintProps) {
    const { t } = useTranslation('lessonPlayer');

    return (
        // `max-w` keeps it inside the viewport on a narrow phone: the panel is anchored to a button a
        // few pixels from the right edge, so its own width is the only thing stopping it running off
        // the left of the screen.
        <div className="absolute right-0 top-full z-50 mt-3 w-80 max-w-[calc(100vw-1.5rem)] rounded-xl border border-brand/30 bg-card p-4 shadow-xl">
            {/* The arrow, sitting on the AI button — 3.75rem from the right edge is that button's centre,
                given two 2rem buttons with a 0.5rem gap. A rotated square rather than the usual border
                trick, so it inherits the panel's border and fill and stays right in either theme. */}
            <div
                aria-hidden
                className="absolute -top-1.5 right-[3.75rem] size-3 rotate-45 border-l border-t border-brand/30 bg-card"
            />

            <div className="flex items-start gap-3">
                <div className="grid size-9 shrink-0 place-items-center rounded-lg border border-brand/20 bg-gradient-to-br from-brand/20 to-brand/5 text-brand">
                    <Sparkles className="size-5" />
                </div>

                <div className="min-w-0 flex-1">
                    <p className="font-heading text-base font-semibold text-foreground">
                        {t('assistantHint.title')}
                    </p>
                    <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
                        {t('assistantHint.body')}
                    </p>
                    <button
                        type="button"
                        onClick={onDismiss}
                        className="mt-3 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-brand-foreground transition-colors hover:bg-brand/90"
                    >
                        {t('assistantHint.dismiss')}
                    </button>
                </div>

                <button
                    type="button"
                    onClick={onDismiss}
                    aria-label={t('common:actions.close')}
                    className="shrink-0 rounded p-0.5 text-muted-foreground transition-colors hover:text-foreground"
                >
                    <X className="size-4" />
                </button>
            </div>
        </div>
    );
}
