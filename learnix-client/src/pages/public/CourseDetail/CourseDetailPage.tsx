import { Helmet } from 'react-helmet-async';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { Clock, Users, Star, Tag, ArrowLeft, BookOpen, Heart } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useCourseDetail } from '@/hooks/useCourseDetail';
import { useCourseReviews } from '@/hooks/useCourseReviews';
import { useMyReview } from '@/hooks/useMyReview';
import { useMyEnrollments } from '@/hooks/useMyEnrollments';
import { useEnroll } from '@/hooks/useEnroll';
import { useWishlist } from '@/hooks/useWishlist';
import { useAddToWishlist, useRemoveFromWishlist } from '@/hooks/useWishlistMutations';
import { useAuthStore } from '@/store/auth.store';
import { QueryError } from '@/components/common/QueryError';
import { CurriculumAccordion } from './components/CurriculumAccordion';
import { ReviewsList } from './components/ReviewsList';
import { ReviewForm } from './components/ReviewForm';
import { cn } from '@/utils/cn';

export default function CourseDetailPage() {
    const { courseId } = useParams<{ courseId: string }>();
    const user = useAuthStore((s) => s.user);
    const { t } = useTranslation('courseDetail');

    const {
        data: course,
        isLoading: courseLoading,
        isError: courseError,
        refetch: refetchCourse,
    } = useCourseDetail(courseId!);
    const { data: reviewsData } = useCourseReviews(courseId!);
    const { data: myReview } = useMyReview(courseId!);
    const { data: enrollmentsData } = useMyEnrollments();
    const enroll = useEnroll();
    const { isInWishlist } = useWishlist();
    const addToWishlist = useAddToWishlist();
    const removeFromWishlist = useRemoveFromWishlist();

    const isEnrolled = enrollmentsData?.items.some((e) => e.courseId === courseId);

    const isOwnCourse = !!user && !!course && user.id === course.instructorId;
    const inWishlist = isInWishlist(courseId!);
    const isFree = course ? course.price === 0 : false;
    const totalLessons = course?.sections.reduce((sum, s) => sum + s.lessons.length, 0) ?? 0;

    const navigate = useNavigate();

    function handleEnroll() {
        if (!courseId) return;
        if (isFree) {
            enroll.mutate(courseId);
        } else {
            navigate(`/payment/${courseId}`);
        }
    }

    if (courseLoading) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-12">
                <div className="animate-pulse space-y-6">
                    <div className="h-8 w-3/4 rounded bg-muted" />
                    <div className="h-4 w-1/2 rounded bg-muted" />
                    <div className="h-64 rounded-xl bg-muted" />
                    <div className="h-48 rounded-xl bg-muted" />
                </div>
            </div>
        );
    }

    if (courseError) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-12">
                <Link
                    to="/courses"
                    className="mb-6 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
                >
                    <ArrowLeft className="h-4 w-4" />
                    {t('backToCatalog')}
                </Link>
                <QueryError
                    message={t('error.title')}
                    onRetry={refetchCourse}
                    retryLabel={t('error.retry')}
                />
            </div>
        );
    }

    if (!course) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-20 text-center">
                <p className="text-muted-foreground">{t('notFound')}</p>
                <Link to="/courses" className="mt-4 inline-block text-primary hover:underline">
                    {t('backToCatalog')}
                </Link>
            </div>
        );
    }

    const description = course.description.slice(0, 160);
    const ogTitle = `${course.title} — Learnix`;

    return (
        <>
            <Helmet>
                <title>{ogTitle}</title>
                <meta name="description" content={description} />
                <meta property="og:title" content={ogTitle} />
                <meta property="og:description" content={description} />
                {course.coverImageUrl && (
                    <meta property="og:image" content={course.coverImageUrl} />
                )}
                <meta property="og:type" content="article" />
            </Helmet>
            <div className="mx-auto max-w-5xl px-6 py-12">
                <Link
                    to="/courses"
                    className="mb-6 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
                >
                    <ArrowLeft className="h-4 w-4" />
                    {t('backToCatalog')}
                </Link>

                <div className="flex flex-col gap-8 lg:grid lg:grid-cols-[1fr_320px]">
                    {/* Main content */}
                    <div className="order-2 min-w-0 space-y-8 lg:order-1">
                        {/* Header */}
                        <div>
                            <h1 className="font-heading text-3xl font-bold text-foreground">
                                {course.title}
                            </h1>

                            {/* Meta */}
                            <div className="mt-3 flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                                <div className="flex items-center gap-1">
                                    <Star className="h-4 w-4 fill-warning text-warning" />
                                    <span className="font-medium text-foreground">
                                        {course.reviewsCount > 0
                                            ? course.averageRating.toFixed(1)
                                            : '—'}
                                    </span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <Users className="h-4 w-4" />
                                    <span>{course.enrollmentsCount} students</span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <Clock className="h-4 w-4" />
                                    <span>{totalLessons} lessons</span>
                                </div>
                            </div>

                            {/* Tags */}
                            {course.tags.length > 0 && (
                                <div className="mt-3 flex flex-wrap gap-2">
                                    {course.tags.map((tag) => (
                                        <span
                                            key={tag}
                                            className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs text-muted-foreground"
                                        >
                                            <Tag className="h-3 w-3" />
                                            {tag}
                                        </span>
                                    ))}
                                </div>
                            )}

                            <p className="mt-4 text-muted-foreground">{course.description}</p>
                        </div>

                        {/* Curriculum */}
                        {course.sections.length > 0 ? (
                            <CurriculumAccordion sections={course.sections} />
                        ) : (
                            <p className="text-sm text-muted-foreground">{t('curriculum.empty')}</p>
                        )}

                        {/* Reviews */}
                        <ReviewsList
                            reviews={reviewsData?.items ?? []}
                            averageRating={course.averageRating}
                            totalCount={course.reviewsCount}
                        />

                        {/* Review form */}
                        {user && !isOwnCourse && isEnrolled && (
                            <ReviewForm courseId={courseId!} existing={myReview ?? null} />
                        )}
                        {user && !isOwnCourse && !isEnrolled && (
                            <p className="text-sm text-muted-foreground">
                                {t('reviews.enrollToReview')}
                            </p>
                        )}
                    </div>

                    {/* Sidebar card */}
                    <aside className="order-1 shrink-0 lg:order-2">
                        <div className="sticky top-24 rounded-xl border border-border bg-card p-6 shadow-sm">
                            {/* Cover image */}
                            {course.coverImageUrl ? (
                                <img
                                    src={course.coverImageUrl}
                                    alt={course.title}
                                    className="mb-5 aspect-video w-full rounded-lg object-cover"
                                />
                            ) : (
                                <div className="mb-5 flex aspect-video w-full items-center justify-center rounded-lg bg-muted">
                                    <BookOpen className="h-12 w-12 text-muted-foreground/40" />
                                </div>
                            )}

                            {/* Price */}
                            <p
                                className={cn(
                                    'font-heading text-3xl font-bold',
                                    isFree ? 'text-success' : 'text-foreground',
                                )}
                            >
                                {isFree ? t('price.free') : `$${course.price}`}
                            </p>

                            {/* Enroll button */}
                            {isOwnCourse ? (
                                <div className="mt-4 flex w-full items-center justify-center rounded-lg border border-border bg-muted px-4 py-3 text-sm font-medium text-muted-foreground">
                                    {t('enroll.ownCourse')}
                                </div>
                            ) : isEnrolled ? (
                                <Link
                                    to={`/courses/${courseId}/learn/${course.sections[0]?.lessons[0]?.id ?? ''}`}
                                    className="mt-4 flex w-full items-center justify-center rounded-lg bg-success px-4 py-3 font-medium text-white transition-opacity hover:opacity-90"
                                >
                                    {t('enroll.enrolled')}
                                </Link>
                            ) : user ? (
                                <button
                                    type="button"
                                    onClick={handleEnroll}
                                    disabled={enroll.isPending}
                                    className="mt-4 w-full rounded-lg bg-primary px-4 py-3 font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
                                >
                                    {enroll.isPending
                                        ? t('enroll.enrolling')
                                        : isFree
                                          ? t('enroll.free')
                                          : t('enroll.paid', { price: course.price })}
                                </button>
                            ) : (
                                <Link
                                    to="/login"
                                    className="mt-4 flex w-full items-center justify-center rounded-lg bg-primary px-4 py-3 font-medium text-primary-foreground transition-opacity hover:opacity-90"
                                >
                                    {t('enroll.loginRequired')}
                                </Link>
                            )}

                            {user && !isEnrolled && !isOwnCourse && (
                                <button
                                    type="button"
                                    onClick={() =>
                                        inWishlist
                                            ? removeFromWishlist.mutate(courseId!)
                                            : addToWishlist.mutate(courseId!)
                                    }
                                    disabled={
                                        addToWishlist.isPending || removeFromWishlist.isPending
                                    }
                                    className={cn(
                                        'mt-3 flex w-full items-center justify-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium transition-colors disabled:opacity-50',
                                        inWishlist
                                            ? 'border-destructive/30 bg-destructive/10 text-destructive hover:bg-destructive/20'
                                            : 'border-border bg-card text-foreground hover:bg-muted',
                                    )}
                                >
                                    <Heart
                                        className={cn(
                                            'h-4 w-4',
                                            inWishlist && 'fill-destructive text-destructive',
                                        )}
                                    />
                                    {addToWishlist.isPending
                                        ? t('wishlist.saving')
                                        : removeFromWishlist.isPending
                                          ? t('wishlist.removing')
                                          : inWishlist
                                            ? t('wishlist.saved')
                                            : t('wishlist.save')}
                                </button>
                            )}

                            <div className="mt-5 border-t border-border pt-4 text-sm text-muted-foreground">
                                <p>
                                    <span className="font-medium text-foreground">
                                        {t('instructor.label')}
                                    </span>
                                    {course.instructorFullName && (
                                        <Link
                                            to={`/instructors/${course.instructorId}`}
                                            className="ml-1 text-primary hover:underline"
                                        >
                                            {course.instructorFullName}
                                        </Link>
                                    )}
                                </p>
                            </div>
                        </div>
                    </aside>
                </div>
            </div>
        </>
    );
}
