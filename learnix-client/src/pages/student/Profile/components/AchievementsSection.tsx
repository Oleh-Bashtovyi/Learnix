import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { AchievementBadge } from '@/components/common/course/AchievementBadge';
import { ALL_ACHIEVEMENT_CODES } from '@/const/achievements.constants';
import { APP_ROUTES } from '@/routes/paths';
import type { UnlockedAchievementDto } from '@/types/achievement.types';

interface AchievementsSectionProps {
    isLoading: boolean;
    earnedCount: number;
    unlockedMap: Map<string, UnlockedAchievementDto>;
}

export function AchievementsSection({
    isLoading,
    earnedCount,
    unlockedMap,
}: AchievementsSectionProps) {
    const { t } = useTranslation('profile');

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
                        <div key={i} className="h-24 animate-pulse rounded-xl bg-muted" />
                    ))}
                </div>
            ) : (
                <div className="mt-4 grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5">
                    {ALL_ACHIEVEMENT_CODES.map((code) => {
                        const unlocked = unlockedMap.get(code);
                        return (
                            <AchievementBadge
                                key={code}
                                code={code}
                                size="sm"
                                unlockedAt={unlocked?.unlockedAt}
                                isNew={unlocked ? !unlocked.seen : false}
                            />
                        );
                    })}
                </div>
            )}

            <div className="mt-4 text-right">
                <Link
                    to={APP_ROUTES.student.achievements}
                    className="text-sm font-medium text-primary hover:underline"
                >
                    {t('achievements.viewAll')}
                </Link>
            </div>
        </section>
    );
}
