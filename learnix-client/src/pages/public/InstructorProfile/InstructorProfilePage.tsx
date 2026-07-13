import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { CourseCard } from '@/components/common/course/CourseCard';
import { Seo } from '@/components/common/seo/Seo';
import { QueryError } from '@/components/common/system/QueryError';
import { BackLink } from '@/components/common/ui/BackLink';
import { Pagination } from '@/components/common/ui/Pagination';
import { TextLink } from '@/components/common/ui/TextLink';
import { INSTRUCTOR_COURSES_PAGE_SIZE } from '@/const/ui.constants';
import { useInstructorCourses } from '@/hooks/instructor/useInstructorCourses';
import { useMediaQuery } from '@/hooks/shared/useMediaQuery';
import { useInstructorProfile } from '@/hooks/user/useInstructorProfile';
import { APP_ROUTES } from '@/routes/paths';
import { isNotFoundError } from '@/utils/errors';
import { InstructorHero } from './InstructorHero';

export default function InstructorProfilePage() {
    const { t } = useTranslation('instructorProfile');
    const { instructorId } = useParams<{ instructorId: string }>();

    const isDesktop = useMediaQuery('(min-width: 640px)');
    const pageSize = isDesktop
        ? INSTRUCTOR_COURSES_PAGE_SIZE.desktop
        : INSTRUCTOR_COURSES_PAGE_SIZE.mobile;

    const [page, setPage] = useState(1);
    const [prevPageSize, setPrevPageSize] = useState(pageSize);

    // Crossing the breakpoint changes how many courses fit on a page, which can leave the reader on a
    // page that no longer exists — page 3 of a six-per-page list is past the end of a twelve-per-page
    // one. Adjusted during render rather than in an effect: React re-renders before painting, so the
    // stale page is never shown, where an effect would render it once and then correct itself.
    if (pageSize !== prevPageSize) {
        setPrevPageSize(pageSize);
        setPage(1);
    }

    const {
        data: profile,
        isLoading: profileLoading,
        error: profileError,
        refetch: refetchProfile,
    } = useInstructorProfile(instructorId!);
    const {
        data: coursesData,
        isLoading: coursesLoading,
        isError: coursesFailed,
        refetch: refetchCourses,
    } = useInstructorCourses(instructorId!, page, pageSize);

    const isLoading = profileLoading || coursesLoading;
    const profileMissing = isNotFoundError(profileError);
    const courses = coursesData?.items ?? [];
    const totalPages = coursesData?.totalPages ?? 1;

    if (isLoading) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-12">
                {/* Shaped like the hero it replaces — a skeleton that settles into a different layout
                    is a flash of the wrong page, not a loading state. */}
                <div className="animate-pulse space-y-6">
                    <div className="space-y-6 rounded-2xl border border-border bg-card p-6 sm:p-8">
                        <div className="flex items-start gap-6">
                            <div className="size-24 shrink-0 rounded-full bg-muted" />
                            <div className="flex-1 space-y-3">
                                <div className="h-7 w-56 rounded bg-muted" />
                                <div className="h-4 w-40 rounded bg-muted" />
                                <div className="h-4 w-full max-w-xl rounded bg-muted" />
                            </div>
                        </div>
                        <div className="grid gap-3 sm:grid-cols-3">
                            {Array.from({ length: 3 }).map((_, i) => (
                                <div key={i} className="h-[76px] rounded-xl bg-muted" />
                            ))}
                        </div>
                    </div>
                    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                        {Array.from({ length: 3 }).map((_, i) => (
                            <div key={i} className="aspect-[4/3] rounded-xl bg-muted" />
                        ))}
                    </div>
                </div>
            </div>
        );
    }

    if (profileMissing) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-20 text-center">
                <Seo title={t('notFound')} noIndex />
                <p className="text-muted-foreground">{t('notFound')}</p>
                <TextLink to={APP_ROUTES.public.courses} className="mt-4 inline-block">
                    {t('common:actions.backToCatalog')}
                </TextLink>
            </div>
        );
    }

    if (!profile) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-12">
                <Seo title={t('error')} noIndex />
                <BackLink
                    fallbackTo={APP_ROUTES.public.courses}
                    fallbackLabel={t('common:actions.backToCatalog')}
                    className="mb-8"
                />
                <QueryError
                    message={t('error')}
                    onRetry={refetchProfile}
                    retryLabel={t('common:actions.tryAgain')}
                />
            </div>
        );
    }

    const fullName = `${profile.firstName} ${profile.lastName}`;

    return (
        <div className="mx-auto max-w-5xl px-6 py-12">
            <Seo
                title={t('seo.title', { name: fullName })}
                description={profile.bio || t('seo.description', { name: fullName })}
                image={profile.avatarUrl}
                type="profile"
                canonicalPath={APP_ROUTES.public.instructorProfile(instructorId!)}
            />
            <BackLink
                fallbackTo={APP_ROUTES.public.courses}
                fallbackLabel={t('common:actions.backToCatalog')}
                className="mb-8"
            />

            <InstructorHero profile={profile} fullName={fullName} />

            {/* Courses */}
            <section>
                <h2 className="mb-5 font-heading text-xl font-semibold text-foreground">
                    {t('coursesHeading')}
                </h2>

                {coursesFailed ? (
                    <QueryError
                        message={t('coursesError')}
                        onRetry={refetchCourses}
                        retryLabel={t('common:actions.tryAgain')}
                    />
                ) : courses.length === 0 ? (
                    <p className="text-sm text-muted-foreground">{t('noCourses')}</p>
                ) : (
                    <>
                        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                            {courses.map((course) => (
                                <CourseCard key={course.id} course={course} hideInstructor />
                            ))}
                        </div>

                        {totalPages > 1 && (
                            <Pagination
                                page={page}
                                totalPages={totalPages}
                                onChange={setPage}
                                className="mt-8"
                            />
                        )}
                    </>
                )}
            </section>
        </div>
    );
}
