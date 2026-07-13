import { useTranslation } from 'react-i18next';
import { Trophy, X } from 'lucide-react';

interface AchievementToastProps {
    name: string;
    description: string;
    onViewAll: () => void;
    onDismiss: () => void;
}

/**
 * Rendered through `toast.custom`, not `toast.success`: an unlocked achievement is the one toast
 * on the platform that is a reward rather than a status report, and sonner's semantic types only
 * offer success / error / warning / info — all of which say "an operation finished", in colours
 * that already mean something else. Hence the achievement palette and the trophy, which lives here
 * as an icon rather than as an emoji inside the translated string.
 */
export function AchievementToast({
    name,
    description,
    onViewAll,
    onDismiss,
}: AchievementToastProps) {
    const { t } = useTranslation('achievements');

    return (
        <div className="flex w-full items-start gap-3 rounded-lg border border-achievement/40 bg-achievement/10 p-4 shadow-lg">
            <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-achievement/15">
                <Trophy className="size-5 text-achievement" />
            </div>

            <div className="min-w-0 flex-1">
                <p className="font-heading text-sm font-semibold text-card-foreground">{name}</p>
                {description && (
                    <p className="mt-0.5 text-xs text-muted-foreground">{description}</p>
                )}
                <button
                    type="button"
                    onClick={onViewAll}
                    className="mt-2 text-xs font-medium text-achievement hover:underline"
                >
                    {t('toast.viewAll')}
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
    );
}
