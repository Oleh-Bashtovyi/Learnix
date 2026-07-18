import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { BarChart3, BookOpen, DollarSign, Star, Users } from 'lucide-react';
import { StatTile } from '@/components/common/elements/StatTile';
import { TextLink } from '@/components/common/elements/TextLink';
import { PAGINATION } from '@/const/ui.constants';
import { CourseStatus } from '@/enums/course.enums';
import { useInstructorOverview } from '@/hooks/instructor/useInstructorAnalytics';
import { useMyCoursesQuery } from '@/hooks/instructor/useMyCoursesQuery';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

function StatSkeleton() {
    return <span className="inline-block h-5 w-12 animate-pulse rounded bg-muted" />;
}

export default function InstructorDashboardPage() {
    const { t } = useTranslation('instructor');
    const { data, isLoading } = useMyCoursesQuery({ take: PAGINATION.DASHBOARD_RECENT });
    const { data: overview, isLoading: overviewLoading } = useInstructorOverview();

    const STATUS_LABELS: Record<CourseStatus, string> = {
        Published: t('common:status.published'),
        Draft: t('common:status.draft'),
        Archived: t('common:status.archived'),
    };

    const recentCourses = data?.items ?? [];
    const totalCourses = data?.totalCount ?? 0;
    const summary = overview?.summary;

    const revenue = (summary?.totalRevenue ?? 0).toLocaleString(undefined, {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 0,
    });

    return (
        <div className="p-8">
            {/* Header — the single New-course entry point (the sidebar has the rest). */}
            <div className="mb-8 flex items-end justify-between">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {t('dashboardTitle')}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{t('dashboardSubtitle')}</p>
                </div>
                <Link
                    to={APP_ROUTES.instructor.newCourse}
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                >
                    {t('btnNewCourse')}
                </Link>
            </div>

            {/* Stats — accurate totals from the analytics summary, one coloured tile (revenue). */}
            <dl className="mb-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <StatTile
                    icon={<BookOpen className="size-5" />}
                    tone="neutral"
                    surface="card"
                    label={t('statTotalCourses')}
                    value={isLoading ? <StatSkeleton /> : totalCourses.toLocaleString()}
                />
                <StatTile
                    icon={<Users className="size-5" />}
                    tone="accent"
                    surface="card"
                    label={t('statTotalStudents')}
                    value={
                        overviewLoading ? (
                            <StatSkeleton />
                        ) : (
                            (summary?.totalStudents ?? 0).toLocaleString()
                        )
                    }
                />
                <StatTile
                    icon={<DollarSign className="size-5" />}
                    tone="brand"
                    surface="card"
                    label={t('statRevenue')}
                    value={overviewLoading ? <StatSkeleton /> : revenue}
                />
                <StatTile
                    icon={<Star className="size-5" />}
                    tone="warning"
                    surface="card"
                    label={t('statAvgRating')}
                    value={
                        overviewLoading ? (
                            <StatSkeleton />
                        ) : (
                            (summary?.averageRating ?? 0).toFixed(2)
                        )
                    }
                />
            </dl>

            {/* Shortcuts — no duplicate "new course"; surface the two destinations worth a jump. */}
            <div className="mb-8 grid gap-4 md:grid-cols-2">
                <ShortcutCard
                    to={APP_ROUTES.instructor.courses}
                    icon={<BookOpen size={20} className="text-primary" />}
                    title={t('myCoursesTitle')}
                    description={t('quickMyCoursesDesc')}
                />
                <ShortcutCard
                    to={APP_ROUTES.instructor.analytics}
                    icon={<BarChart3 size={20} className="text-primary" />}
                    title={t('quickAnalyticsTitle')}
                    description={t('quickAnalyticsDesc')}
                />
            </div>

            {/* Recent courses */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <div className="flex items-center justify-between border-b border-border px-5 py-4">
                    <h3 className="font-heading font-semibold text-foreground">
                        {t('recentCoursesTitle')}
                    </h3>
                    {totalCourses > 0 && (
                        <TextLink to={APP_ROUTES.instructor.courses} className="text-sm">
                            {t('recentCoursesViewAll')}
                        </TextLink>
                    )}
                </div>

                {isLoading ? (
                    <div className="py-12 text-center text-sm text-muted-foreground">
                        {t('recentLoading')}
                    </div>
                ) : recentCourses.length === 0 ? (
                    <div className="py-12 text-center">
                        <p className="text-sm text-muted-foreground">{t('dashboardEmpty')}</p>
                        <TextLink
                            to={APP_ROUTES.instructor.newCourse}
                            className="mt-3 inline-block text-sm"
                        >
                            {t('dashboardEmptyCta')}
                        </TextLink>
                    </div>
                ) : (
                    <ul className="divide-y divide-border">
                        {recentCourses.map((course) => (
                            <li
                                key={course.id}
                                className="flex items-center gap-4 px-5 py-3 hover:bg-secondary/30"
                            >
                                <div className="h-10 w-14 shrink-0 overflow-hidden rounded bg-gradient-to-br from-primary/30 to-accent/30">
                                    {course.coverImageUrl && (
                                        <img
                                            src={course.coverImageUrl}
                                            alt=""
                                            className="size-full object-cover"
                                        />
                                    )}
                                </div>
                                <div className="min-w-0 flex-1">
                                    <p className="truncate font-medium text-foreground">
                                        {course.title}
                                    </p>
                                    <p className="text-xs text-muted-foreground">
                                        {t('studentCount', { count: course.enrollmentsCount })}
                                    </p>
                                </div>
                                <span
                                    className={cn(
                                        'shrink-0 rounded px-2 py-0.5 text-xs font-medium',
                                        STATUS_STYLES[course.status],
                                    )}
                                >
                                    {STATUS_LABELS[course.status]}
                                </span>
                                <Link
                                    to={APP_ROUTES.instructor.editCourse(course.id)}
                                    className="shrink-0 text-xs text-muted-foreground hover:text-primary"
                                >
                                    {t('common:actions.edit')}
                                </Link>
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
}

interface ShortcutCardProps {
    to: string;
    icon: ReactNode;
    title: string;
    description: string;
}

function ShortcutCard({ to, icon, title, description }: ShortcutCardProps) {
    return (
        <Link
            to={to}
            className="flex items-center gap-3 rounded-xl border border-dashed border-border bg-card p-5 transition-colors hover:border-primary/50 hover:bg-primary/5"
        >
            {icon}
            <div>
                <p className="font-medium text-foreground">{title}</p>
                <p className="text-xs text-muted-foreground">{description}</p>
            </div>
        </Link>
    );
}
