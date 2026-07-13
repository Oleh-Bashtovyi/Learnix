import { useTranslation } from 'react-i18next';
import { Lock } from 'lucide-react';
import { ACHIEVEMENT_IMAGES } from '@/assets/achievements';
import { ACHIEVEMENT_META } from '@/const/achievements.constants';
import { cn } from '@/utils/cn';

interface AchievementBadgeProps {
    code: string;
    unlockedAt?: string;
    isNew?: boolean;
    /** Custom cover art — replaces the gradient+icon placeholder */
    imageUrl?: string;
    /** 'md' (default) — full card with description and date; 'sm' — compact for profile grid */
    size?: 'sm' | 'md';
    onClick?: () => void;
    className?: string;
}

export function AchievementBadge({
    code,
    unlockedAt,
    isNew,
    imageUrl,
    size = 'md',
    onClick,
    className,
}: AchievementBadgeProps) {
    const { t, i18n } = useTranslation('achievements');
    const meta = ACHIEVEMENT_META[code];
    const isUnlocked = !!unlockedAt;
    const Icon = meta?.icon ?? Lock;
    const isSm = size === 'sm';

    const resolvedImage = imageUrl ?? ACHIEVEMENT_IMAGES[code];

    // The gradient is each achievement's own colour. It used to paint the icon circle, which meant it
    // was never seen at all: there is cover art for every achievement, so that branch was unreachable.
    // It now sits *behind* the art as a glow, and only when the badge is unlocked — which is the one
    // job it was always meant to do, telling an earned badge apart from one that is still locked.
    const gradient = meta?.gradient;

    const glowStyle = gradient
        ? { background: `linear-gradient(135deg, ${gradient[0]}, ${gradient[1]})` }
        : undefined;

    const iconAreaStyle = !resolvedImage && isUnlocked ? glowStyle : undefined;

    const name = t(`meta.${code}.name`, { defaultValue: code });
    const description = t(`meta.${code}.description`, { defaultValue: '' });

    return (
        <button
            type="button"
            onClick={onClick}
            title={
                !isUnlocked
                    ? t('achievements.lockedHint', {
                          defaultValue: 'Keep learning to unlock this achievement!',
                      })
                    : undefined
            }
            className={cn(
                'group relative flex flex-col items-center rounded-xl border text-center transition-all duration-300',
                isSm ? 'gap-2 p-3' : 'gap-3 p-5',
                isUnlocked
                    ? 'border-accent/30 bg-accent/5 shadow-[0_0_15px_rgba(0,0,0,0)] hover:border-accent/60 hover:bg-accent/10 hover:shadow-[0_0_20px_rgba(var(--accent),0.15)]'
                    : 'border-border/50 bg-muted/20 opacity-60 hover:bg-muted/40 hover:opacity-100',
                onClick ? 'cursor-pointer' : 'cursor-default',
                className,
            )}
        >
            {/* "New" badge — z-10 keeps it above the art. Filters and opacity below it create stacking
                contexts that browsers paint as if z-index: 0, and this span comes first in the DOM, so
                without a z-index of its own it loses to them and disappears under the badge it labels. */}
            {isNew && (
                <span
                    className={cn(
                        'absolute z-10 rounded-full bg-accent font-semibold text-accent-foreground',
                        isSm
                            ? 'right-1.5 top-1.5 px-1.5 py-px text-[10px]'
                            : 'right-2 top-2 px-2 py-0.5 text-xs',
                    )}
                >
                    {t('badge.new')}
                </span>
            )}

            {/* Cover art, over the achievement's own glow */}
            <div className={cn('relative shrink-0', isSm ? 'size-11' : 'size-14')}>
                {isUnlocked && glowStyle && (
                    <div
                        aria-hidden
                        style={glowStyle}
                        className="absolute inset-0 scale-110 rounded-full opacity-50 blur-md transition-opacity duration-300 group-hover:opacity-80"
                    />
                )}

                <div
                    className={cn(
                        'relative flex size-full items-center justify-center overflow-hidden rounded-full',
                        !resolvedImage && !isUnlocked && 'bg-muted text-muted-foreground',
                    )}
                    style={iconAreaStyle}
                >
                    {resolvedImage ? (
                        <img
                            src={resolvedImage}
                            alt={name}
                            className={cn(
                                'size-full object-cover transition-all duration-500',
                                !isUnlocked && 'opacity-30 grayscale sepia-[0.3]',
                            )}
                        />
                    ) : isUnlocked ? (
                        <Icon className={cn('text-white', isSm ? 'size-5' : 'size-7')} />
                    ) : (
                        <Lock className={cn(isSm ? 'size-4' : 'size-6')} />
                    )}
                </div>
            </div>

            {/* Text */}
            <div className={cn('w-full min-w-0', !isSm && 'space-y-1')}>
                <p
                    className={cn(
                        'font-heading font-semibold leading-tight text-foreground',
                        isSm ? 'line-clamp-2 text-[11px]' : 'text-sm',
                    )}
                >
                    {name}
                </p>

                {!isSm && (
                    <>
                        <p className="text-xs text-muted-foreground">{description}</p>
                        {isUnlocked && unlockedAt && (
                            <p className="mt-1 text-xs text-accent">
                                {t('badge.earnedOn', {
                                    date: new Date(unlockedAt).toLocaleDateString(i18n.language, {
                                        month: 'short',
                                        day: 'numeric',
                                        year: 'numeric',
                                    }),
                                })}
                            </p>
                        )}
                    </>
                )}
            </div>
        </button>
    );
}
