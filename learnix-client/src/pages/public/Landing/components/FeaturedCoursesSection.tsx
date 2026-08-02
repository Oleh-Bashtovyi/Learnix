import { useTranslation } from 'react-i18next';
import { BookOpen } from 'lucide-react';
import { CourseCard } from '@/components/common/course/CourseCard';
import { TextLink } from '@/components/common/elements/TextLink';
import { QueryError } from '@/components/common/system/QueryError';
import { useCourseCount } from '@/hooks/course/useCourseCount';
import { useFeaturedCourses } from '@/hooks/course/useFeaturedCourses';
import { APP_ROUTES } from '@/routes/paths';

export function FeaturedCoursesSection() {
    const { t } = useTranslation('landing');
    const { data: courses = [], isLoading, isError, refetch: onRetry } = useFeaturedCourses();
    const { data: totalCount } = useCourseCount();

    // Same rule as the categories above: no invitation to a catalog that is not answering.
    const hasContent = !isLoading && !isError && courses.length > 0;

    const viewAllLabel =
        totalCount !== undefined
            ? t('featuredCourses.viewMore', { count: totalCount })
            : t('featuredCourses.viewAll');

    const renderContent = () => {
        if (isLoading) {
            return (
                <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                    {Array.from({ length: 6 }).map((_, i) => (
                        <div
                            key={i}
                            className="h-72 animate-pulse rounded-xl border border-border bg-card"
                        />
                    ))}
                </div>
            );
        }

        if (isError) {
            return (
                <QueryError
                    message={t('featuredCourses.error')}
                    onRetry={onRetry}
                    retryLabel={t('common:actions.tryAgain')}
                    className="min-h-0 py-10"
                />
            );
        }

        if (courses.length === 0) {
            return (
                <div className="flex min-h-[200px] flex-col items-center justify-center gap-2 text-center">
                    <BookOpen className="size-10 text-muted-foreground/40" />
                    <p className="text-sm text-muted-foreground">{t('featuredCourses.empty')}</p>
                </div>
            );
        }

        return (
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                {courses.map((course) => (
                    <CourseCard key={course.id} course={course} />
                ))}
            </div>
        );
    };

    return (
        <section id="courses" className="bg-secondary/40 pb-20 pt-12">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-8 flex flex-col gap-4 sm:mb-10 sm:flex-row sm:items-end sm:justify-between">
                    <div>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            {t('featuredCourses.heading')}
                        </h2>
                        <p className="mt-2 text-muted-foreground">
                            {t('featuredCourses.subtitle')}
                        </p>
                    </div>
                    {/* One link per breakpoint: this one on desktop, the one below the grid on mobile.
                        Two identical links to the same page is not emphasis, it is noise. */}
                    {hasContent && (
                        <TextLink
                            to={APP_ROUTES.public.courses}
                            className="hidden text-sm sm:inline"
                        >
                            {viewAllLabel}
                        </TextLink>
                    )}
                </div>

                {renderContent()}

                {hasContent && (
                    <div className="mt-10 text-center sm:hidden">
                        <TextLink
                            to={APP_ROUTES.public.courses}
                            className="inline-flex items-center gap-2 text-sm"
                        >
                            {viewAllLabel}
                        </TextLink>
                    </div>
                )}
            </div>
        </section>
    );
}
