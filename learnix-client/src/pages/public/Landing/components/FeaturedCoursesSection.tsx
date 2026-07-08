import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { BookOpen } from 'lucide-react';
import { CourseCard } from '@/components/common/course/CourseCard';
import { QueryError } from '@/components/common/system/QueryError';
import { APP_ROUTES } from '@/routes/paths';
import type { CourseSummaryDto } from '@/types/course.types';

interface FeaturedCoursesSectionProps {
    courses: CourseSummaryDto[];
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    totalCount?: number;
}

export function FeaturedCoursesSection({
    courses,
    isLoading,
    isError,
    onRetry,
    totalCount,
}: FeaturedCoursesSectionProps) {
    const { t } = useTranslation('landing');

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
                    <Link
                        to={APP_ROUTES.public.courses}
                        className="text-sm font-medium text-primary hover:underline"
                    >
                        {t('featuredCourses.viewAll')}
                    </Link>
                </div>

                {renderContent()}

                <div className="mt-10 text-center">
                    <Link
                        to={APP_ROUTES.public.courses}
                        className="inline-flex items-center gap-2 font-medium text-primary hover:underline"
                    >
                        {totalCount !== undefined
                            ? t('featuredCourses.viewMore', { count: totalCount })
                            : t('featuredCourses.viewAll')}
                    </Link>
                </div>
            </div>
        </section>
    );
}
