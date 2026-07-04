import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { BookOpen, Globe, GraduationCap } from 'lucide-react';
import { notificationsApi } from '@/api/notifications.api';
import { queryKeys } from '@/api/queryKeys';
import { AchievementBadge } from '@/components/common/course/AchievementBadge';
import { ALL_ACHIEVEMENT_CODES } from '@/const/achievements.constants';
import { useMarkAchievementSeen } from '@/hooks/user/useMarkAchievementSeen';
import { useMyAchievements } from '@/hooks/user/useMyAchievements';

export default function AchievementsPage() {
    const { t } = useTranslation('achievements');
    const { data, isLoading } = useMyAchievements();
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
            <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6 sm:py-12">
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

    const progress = data?.progress;

    return (
        <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6 sm:py-12">
            <div>
                <h1 className="font-heading text-2xl font-bold text-foreground sm:text-3xl">
                    {t('page.title')}
                </h1>
                <p className="mt-1 text-muted-foreground">{t('page.subtitle')}</p>
            </div>

            {/* Progress stats */}
            {progress && (
                <section className="mt-6 rounded-xl border border-border bg-card p-4 sm:mt-8 sm:p-6">
                    <h2 className="font-heading text-lg font-semibold">
                        {t('page.progressSection')}
                    </h2>
                    <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3 sm:gap-4">
                        <div className="flex items-center justify-between gap-2 rounded-lg bg-muted/50 p-4 text-left sm:flex-col sm:items-center sm:justify-start sm:text-center">
                            <BookOpen className="size-6 text-primary" />
                            <span className="font-heading text-2xl font-bold text-foreground">
                                {progress.lessonsCompleted}
                            </span>
                            <span className="text-xs text-muted-foreground">
                                {t('page.statsLessons')}
                            </span>
                        </div>
                        <div className="flex items-center justify-between gap-2 rounded-lg bg-muted/50 p-4 text-left sm:flex-col sm:items-center sm:justify-start sm:text-center">
                            <GraduationCap className="size-6 text-accent" />
                            <span className="font-heading text-2xl font-bold text-foreground">
                                {progress.coursesCompleted}
                            </span>
                            <span className="text-xs text-muted-foreground">
                                {t('page.statsCourses')}
                            </span>
                        </div>
                        <div className="flex items-center justify-between gap-2 rounded-lg bg-muted/50 p-4 text-left sm:flex-col sm:items-center sm:justify-start sm:text-center">
                            <Globe className="size-6 text-success" />
                            <span className="font-heading text-2xl font-bold text-foreground">
                                {progress.distinctCategoriesCompleted}
                            </span>
                            <span className="text-xs text-muted-foreground">
                                {t('page.statsCategories')}
                            </span>
                        </div>
                    </div>
                </section>
            )}

            {/* Earned count */}
            <p className="mt-6 text-sm font-medium text-muted-foreground sm:mt-8">
                {t('page.earnedCount', { count: data?.unlocked.length ?? 0 })}
            </p>

            {/* Achievement grid */}
            <div className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4 xl:grid-cols-5">
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
