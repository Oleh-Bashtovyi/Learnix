import { useTranslation } from 'react-i18next';
import { AchievementBadge } from '@/components/common/course/AchievementBadge';
import { TextLink } from '@/components/common/ui/TextLink';
import {
    ALL_ACHIEVEMENT_CODES,
    PROFILE_MOBILE_VISIBLE_ACHIEVEMENTS,
} from '@/const/achievements.constants';
import { APP_ROUTES } from '@/routes/paths';
import type { UnlockedAchievementDto } from '@/types/achievement.types';
import { cn } from '@/utils/cn';

interface AchievementsSectionProps {
    isLoading: boolean;
    earnedCount: number;
    unlockedMap: Map<string, UnlockedAchievementDto>;
}

/** Unseen first, then the rest of the unlocked ones, then locked. */
function sortRank(unlocked: UnlockedAchievementDto | undefined): number {
    if (!unlocked) return 2;
    return unlocked.seen ? 1 : 0;
}

export function AchievementsSection({
    isLoading,
    earnedCount,
    unlockedMap,
}: AchievementsSectionProps) {
    const { t } = useTranslation('profile');

    // Mobile only shows the first PROFILE_MOBILE_VISIBLE_ACHIEVEMENTS tiles, so order matters:
    // a freshly unlocked badge must never be one of the ones that gets cut off.
    const orderedCodes = [...ALL_ACHIEVEMENT_CODES].sort(
        (a, b) => sortRank(unlockedMap.get(a)) - sortRank(unlockedMap.get(b)),
    );

    return (
        <section className="rounded-xl border border-border bg-card p-4 sm:p-6">
            <div className="flex flex-col justify-between gap-2 sm:flex-row sm:items-center sm:gap-0">
                <h2 className="font-heading text-lg font-semibold">
                    {t('achievements.sectionTitle')}
                </h2>
                <span className="text-sm text-muted-foreground">
                    {t('achievements.earnedCount', {
                        earned: earnedCount,
                        total: ALL_ACHIEVEMENT_CODES.length,
                    })}
                </span>
            </div>

            {isLoading ? (
                <div className="mt-4 grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5">
                    {Array.from({ length: ALL_ACHIEVEMENT_CODES.length }).map((_, i) => (
                        <div
                            key={i}
                            className={cn(
                                'h-24 animate-pulse rounded-xl bg-muted',
                                i >= PROFILE_MOBILE_VISIBLE_ACHIEVEMENTS && 'hidden sm:block',
                            )}
                        />
                    ))}
                </div>
            ) : (
                <div className="mt-4 grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5">
                    {orderedCodes.map((code, index) => {
                        const unlocked = unlockedMap.get(code);
                        return (
                            <AchievementBadge
                                key={code}
                                code={code}
                                size="sm"
                                unlockedAt={unlocked?.unlockedAt}
                                isNew={unlocked ? !unlocked.seen : false}
                                className={cn(
                                    index >= PROFILE_MOBILE_VISIBLE_ACHIEVEMENTS &&
                                        'hidden sm:flex',
                                )}
                            />
                        );
                    })}
                </div>
            )}

            <div className="mt-4 text-right">
                <TextLink to={APP_ROUTES.student.achievements} className="text-sm">
                    {t('achievements.viewAll')}
                </TextLink>
            </div>
        </section>
    );
}
