import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { BookOpen, Globe, GraduationCap } from 'lucide-react';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';
import { AchievementBadge } from '@/components/common/course/AchievementBadge';
import { QueryError } from '@/components/common/system/QueryError';
import { HeroPanel } from '@/components/common/ui/HeroPanel';
import { StatTile } from '@/components/common/ui/StatTile';
import { ALL_ACHIEVEMENT_CODES } from '@/const/achievements.constants';
import { useMarkAchievementSeen } from '@/hooks/user/useMarkAchievementSeen';
import { useMyAchievements } from '@/hooks/user/useMyAchievements';

export default function AchievementsPage() {
    const { t } = useTranslation('achievements');
    const { data, isLoading, isError, refetch } = useMyAchievements();
    const markSeen = useMarkAchievementSeen();
    const queryClient = useQueryClient();

    const unlockedMap = new Map(data?.unlocked.map((a) => [a.code, a]));
    const unseenIds = data?.unlocked.filter((a) => !a.seen).map((a) => a.id) ?? [];

    useEffect(() => {
        if (unseenIds.length > 0) {
            unseenIds.forEach((id) => markSeen.mutate(id));
        }
    }, [unseenIds.join(',')]); // eslint-disable-line react-hooks/exhaustive-deps

    useEffect(() => {
        // Mark all achievement notifications as read when visiting this page
        notificationsApi
            .markReadByType('AchievementEarned')
            .then(() => {
                queryClient.invalidateQueries({ queryKey: queryKeys.notifications.unreadCount() });
                queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list() });
            })
            .catch(() => {});
    }, [queryClient]);

    if (isLoading) {
        return (
            <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
                <div className="animate-pulse space-y-6">
                    <div className="h-8 w-48 rounded bg-muted" />
                    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4">
                        {Array.from({ length: 10 }).map((_, i) => (
                            <div key={i} className="h-40 rounded-xl bg-muted" />
                        ))}
                    </div>
                </div>
            </div>
        );
    }

    // Ahead of the badge grid: without data every badge renders locked and the counter reads
    // zero, which is indistinguishable from a student who has earned nothing.
    if (isError) {
        return (
            <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
                <QueryError
                    message={t('error.title')}
                    onRetry={refetch}
                    retryLabel={t('common:actions.tryAgain')}
                />
            </div>
        );
    }

    const progress = data?.progress;
    const earnedCount = data?.unlocked.length ?? 0;
    const earnedPercent = Math.round((earnedCount / ALL_ACHIEVEMENT_CODES.length) * 100);

    return (
        <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
            {/* No heading or back link of its own: this is a tab inside StudentDashboardLayout now,
                and the layout owns both the page title and the way back out of it. */}
            <HeroPanel>
                <p className="text-sm text-muted-foreground">{t('page.subtitle')}</p>

                {/* The badge count leads, and it is the one figure with a ceiling — so it is the one
                    that gets a bar. The three below it are open-ended counts; a bar on them would be a
                    bar against nothing. */}
                <div className="mt-4 flex items-baseline gap-2">
                    <span className="font-heading text-4xl font-bold text-foreground">
                        {earnedCount}
                    </span>
                    <span className="text-lg text-muted-foreground">
                        / {ALL_ACHIEVEMENT_CODES.length}
                    </span>
                    <span className="ml-1 text-sm text-muted-foreground">
                        {t('page.unlockedLabel')}
                    </span>
                </div>

                <div className="mt-3 h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                        className="h-full rounded-full bg-gradient-to-r from-brand to-accent transition-[width] duration-700"
                        style={{ width: `${earnedPercent}%` }}
                    />
                </div>

                {progress && (
                    <dl className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-3">
                        <StatTile
                            icon={<BookOpen className="size-5" />}
                            tone="brand"
                            label={t('page.statsLessons')}
                            value={String(progress.lessonsCompleted)}
                        />
                        <StatTile
                            icon={<GraduationCap className="size-5" />}
                            tone="accent"
                            label={t('page.statsCourses')}
                            value={String(progress.coursesCompleted)}
                        />
                        <StatTile
                            icon={<Globe className="size-5" />}
                            tone="success"
                            label={t('page.statsCategories')}
                            value={String(progress.distinctCategoriesCompleted)}
                        />
                    </dl>
                )}
            </HeroPanel>

            {/* Achievement grid */}
            <div className="mt-8 grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4 xl:grid-cols-5">
                {ALL_ACHIEVEMENT_CODES.map((code) => {
                    const unlocked = unlockedMap.get(code);
                    return (
                        <AchievementBadge
                            key={code}
                            code={code}
                            unlockedAt={unlocked?.unlockedAt}
                            isNew={unlocked ? !unlocked.seen : false}
                        />
                    );
                })}
            </div>
        </div>
    );
}
