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
    const { t } = useTranslation('achievements');
    const meta = ACHIEVEMENT_META[code];
    const isUnlocked = !!unlockedAt;
    const Icon = meta?.icon ?? Lock;
    const isSm = size === 'sm';

    const resolvedImage = imageUrl ?? ACHIEVEMENT_IMAGES[code];

    const iconAreaStyle =
        !resolvedImage && isUnlocked && meta?.gradient
            ? { background: `linear-gradient(135deg, ${meta.gradient[0]}, ${meta.gradient[1]})` }
            : undefined;

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
            {/* "New" badge */}
            {isNew && (
                <span
                    className={cn(
                        'absolute rounded-full bg-accent font-semibold text-accent-foreground',
                        isSm
                            ? 'right-1.5 top-1.5 px-1.5 py-px text-[10px]'
                            : 'right-2 top-2 px-2 py-0.5 text-xs',
                    )}
                >
                    New
                </span>
            )}

            {/* Cover image / gradient placeholder */}
            <div
                className={cn(
                    'flex shrink-0 items-center justify-center overflow-hidden rounded-full',
                    isSm ? 'h-11 w-11' : 'h-14 w-14',
                    !resolvedImage && !isUnlocked && 'bg-muted text-muted-foreground',
                )}
                style={iconAreaStyle}
            >
                {resolvedImage ? (
                    <img
                        src={resolvedImage}
                        alt={name}
                        className={cn(
                            'h-full w-full object-cover transition-all duration-500',
                            !isUnlocked && 'opacity-30 grayscale sepia-[0.3]',
                            isUnlocked && 'drop-shadow-lg',
                        )}
                    />
                ) : isUnlocked ? (
                    <Icon className={cn('text-white', isSm ? 'h-5 w-5' : 'h-7 w-7')} />
                ) : (
                    <Lock className={cn(isSm ? 'h-4 w-4' : 'h-6 w-6')} />
                )}
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
                                Earned{' '}
                                {new Date(unlockedAt).toLocaleDateString('en-US', {
                                    month: 'short',
                                    day: 'numeric',
                                    year: 'numeric',
                                })}
                            </p>
                        )}
                    </>
                )}
            </div>
        </button>
    );
}
