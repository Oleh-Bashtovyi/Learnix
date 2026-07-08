import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { BookOpen, PlusCircle, Users } from 'lucide-react';
import { PAGINATION } from '@/const/ui.constants';
import { CourseStatus } from '@/enums/course.enums';
import { useMyCoursesQuery } from '@/hooks/instructor/useMyCoursesQuery';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

export default function InstructorDashboardPage() {
    const { t } = useTranslation('instructor');
    const { data, isLoading } = useMyCoursesQuery({ take: PAGINATION.DASHBOARD_RECENT });

    const STATUS_LABELS: Record<CourseStatus, string> = {
        Published: t('common:status.published'),
        Draft: t('common:status.draft'),
        Archived: t('common:status.archived'),
    };

    const recentCourses = data?.items ?? [];
    const totalCourses = data?.totalCount ?? 0;
    const totalStudents = recentCourses.reduce((sum, c) => sum + c.enrollmentsCount, 0);

    return (
        <div className="p-8">
            {/* Header */}
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

            {/* Stats */}
            <div className="mb-8 grid gap-4 md:grid-cols-2">
                <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-5">
                    <div className="grid size-10 place-items-center rounded-lg bg-primary/10">
                        <BookOpen size={20} className="text-primary" />
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">{t('statTotalCourses')}</p>
                        <p className="font-heading text-2xl font-bold text-foreground">
                            {totalCourses}
                        </p>
                    </div>
                </div>
                <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-5">
                    <div className="grid size-10 place-items-center rounded-lg bg-primary/10">
                        <Users size={20} className="text-primary" />
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">{t('statTotalStudents')}</p>
                        <p className="font-heading text-2xl font-bold text-foreground">
                            {totalStudents.toLocaleString()}
                        </p>
                    </div>
                </div>
            </div>

            {/* Quick actions */}
            <div className="mb-8 grid gap-4 md:grid-cols-2">
                <Link
                    to={APP_ROUTES.instructor.newCourse}
                    className="flex items-center gap-3 rounded-xl border border-dashed border-border bg-card p-5 transition-colors hover:border-primary/50 hover:bg-primary/5"
                >
                    <PlusCircle size={20} className="text-primary" />
                    <div>
                        <p className="font-medium text-foreground">{t('btnNewCourse')}</p>
                        <p className="text-xs text-muted-foreground">Start creating a new course</p>
                    </div>
                </Link>
                <Link
                    to={APP_ROUTES.instructor.courses}
                    className="flex items-center gap-3 rounded-xl border border-dashed border-border bg-card p-5 transition-colors hover:border-primary/50 hover:bg-primary/5"
                >
                    <BookOpen size={20} className="text-primary" />
                    <div>
                        <p className="font-medium text-foreground">{t('myCoursesTitle')}</p>
                        <p className="text-xs text-muted-foreground">
                            Manage, publish and track courses
                        </p>
                    </div>
                </Link>
            </div>

            {/* Recent courses */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <div className="flex items-center justify-between border-b border-border px-5 py-4">
                    <h3 className="font-heading font-semibold text-foreground">
                        {t('recentCoursesTitle')}
                    </h3>
                    {totalCourses > 0 && (
                        <Link
                            to={APP_ROUTES.instructor.courses}
                            className="text-sm text-primary hover:underline"
                        >
                            {t('recentCoursesViewAll')}
                        </Link>
                    )}
                </div>

                {isLoading ? (
                    <div className="py-12 text-center text-sm text-muted-foreground">
                        Loading...
                    </div>
                ) : recentCourses.length === 0 ? (
                    <div className="py-12 text-center">
                        <p className="text-sm text-muted-foreground">{t('dashboardEmpty')}</p>
                        <Link
                            to={APP_ROUTES.instructor.newCourse}
                            className="mt-3 inline-block text-sm text-primary hover:underline"
                        >
                            {t('dashboardEmptyCta')}
                        </Link>
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
                                        {course.enrollmentsCount} student
                                        {course.enrollmentsCount !== 1 ? 's' : ''}
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
