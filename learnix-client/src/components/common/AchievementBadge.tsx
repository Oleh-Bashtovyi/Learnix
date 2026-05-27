import { format } from 'date-fns';
import { Lock } from 'lucide-react';
import { cn } from '@/utils/cn';
import { ACHIEVEMENT_META } from '@/const/localization/achievements';
import { ACHIEVEMENT_IMAGES } from '@/assets/achievements';

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
    const meta = ACHIEVEMENT_META[code];
    const isUnlocked = !!unlockedAt;
    const Icon = meta?.icon ?? Lock;
    const isSm = size === 'sm';

    const resolvedImage = imageUrl ?? ACHIEVEMENT_IMAGES[code];

    const iconAreaStyle =
        !resolvedImage && isUnlocked && meta?.gradient
            ? { background: `linear-gradient(135deg, ${meta.gradient[0]}, ${meta.gradient[1]})` }
            : undefined;

    return (
        <button
            type="button"
            onClick={onClick}
            className={cn(
                'group relative flex flex-col items-center rounded-xl border text-center transition-all',
                isSm ? 'gap-2 p-3' : 'gap-3 p-5',
                isUnlocked
                    ? 'border-accent/30 bg-accent/5 hover:border-accent/60 hover:bg-accent/10'
                    : 'border-border bg-muted/30 opacity-50',
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
                        alt={meta?.name ?? code}
                        className={cn(
                            'h-full w-full object-cover',
                            !isUnlocked && 'grayscale opacity-40',
                        )}
                    />
                ) : isUnlocked ? (
                    <Icon className={cn('text-white', isSm ? 'h-5 w-5' : 'h-7 w-7')} />
                ) : (
                    <Lock className={cn(isSm ? 'h-4 w-4' : 'h-6 w-6')} />
                )}
            </div>

            {/* Text */}
            <div className={cn('min-w-0 w-full', !isSm && 'space-y-1')}>
                <p
                    className={cn(
                        'font-heading font-semibold leading-tight text-foreground',
                        isSm ? 'line-clamp-2 text-[11px]' : 'text-sm',
                    )}
                >
                    {meta?.name ?? code}
                </p>

                {!isSm && (
                    <>
                        <p className="text-xs text-muted-foreground">{meta?.description}</p>
                        {isUnlocked && unlockedAt && (
                            <p className="mt-1 text-xs text-accent">
                                Earned {format(new Date(unlockedAt), 'MMM d, yyyy')}
                            </p>
                        )}
                    </>
                )}
            </div>
        </button>
    );
}
