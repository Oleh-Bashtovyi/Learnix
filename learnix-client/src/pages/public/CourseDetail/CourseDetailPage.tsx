import { useState } from 'react';
import { Helmet } from 'react-helmet-async';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Clock, Star, Tag, Users } from 'lucide-react';
import { toast } from 'sonner';
import { QueryError } from '@/components/common/system/QueryError';
import { Pagination } from '@/components/common/ui/Pagination';
import { useCourseDetail } from '@/hooks/course/useCourseDetail';
import { useCourseReviews } from '@/hooks/student/useCourseReviews';
import { useEnroll } from '@/hooks/student/useEnroll';
import { useMyEnrollments } from '@/hooks/student/useMyEnrollments';
import { useMyReview } from '@/hooks/student/useMyReview';
import { useWishlist } from '@/hooks/student/useWishlist';
import { useAddToWishlist, useRemoveFromWishlist } from '@/hooks/student/useWishlistMutations';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { CourseSidebar } from './components/CourseSidebar';
import { CurriculumAccordion } from './components/CurriculumAccordion';
import { ReviewForm } from './components/ReviewForm';
import { ReviewsList } from './components/ReviewsList';

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

    const [page, setPage] = useState(1);
    const take = 5; // Show 5 reviews per page
    const skip = (page - 1) * take;

    const { data: reviewsData } = useCourseReviews(courseId!, skip, take);
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
    const location = useLocation();
    const backUrl = (location.state as { from?: string })?.from || APP_ROUTES.public.courses;

    function handleEnroll() {
        if (!courseId) return;
        if (user && !user.emailVerified) {
            toast.error(t('enroll.emailNotVerified'));
            return;
        }
        if (isFree) {
            enroll.mutate(courseId);
        } else {
            navigate(APP_ROUTES.student.payment(courseId));
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
                    to={backUrl}
                    className="mb-6 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
                >
                    <ArrowLeft className="size-4" />
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
                <Link to={backUrl} className="mt-4 inline-block text-primary hover:underline">
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
                    to={backUrl}
                    className="mb-6 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
                >
                    <ArrowLeft className="size-4" />
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
                                    <Star className="size-4 fill-warning text-warning" />
                                    <span className="font-medium text-foreground">
                                        {course.reviewsCount > 0
                                            ? course.averageRating.toFixed(1)
                                            : '—'}
                                    </span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <Users className="size-4" />
                                    <span>{course.enrollmentsCount} students</span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <Clock className="size-4" />
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
                                            <Tag className="size-3" />
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

                        {/* Pagination for reviews */}
                        {reviewsData && reviewsData.totalCount > take && (
                            <Pagination
                                className="mt-8"
                                page={page}
                                totalPages={Math.ceil(reviewsData.totalCount / take)}
                                onChange={setPage}
                            />
                        )}

                        {/* Review form */}
                        {user && !isOwnCourse && isEnrolled && (
                            <ReviewForm courseId={courseId!} existing={myReview ?? null} />
                        )}
                        {user && !isOwnCourse && !isEnrolled && (
                            <div className="mt-8 text-center">
                                <p className="text-sm text-muted-foreground">
                                    {t('reviews.enrollToReview')}
                                </p>
                            </div>
                        )}
                    </div>

                    {/* Sidebar card */}
                    <CourseSidebar
                        course={course}
                        isFree={isFree}
                        isOwnCourse={isOwnCourse}
                        isEnrolled={isEnrolled ?? false}
                        user={user}
                        inWishlist={inWishlist}
                        enrollIsPending={enroll.isPending}
                        onEnroll={handleEnroll}
                        onToggleWishlist={() =>
                            inWishlist
                                ? removeFromWishlist.mutate(courseId!)
                                : addToWishlist.mutate(courseId!)
                        }
                        wishlistIsPending={addToWishlist.isPending || removeFromWishlist.isPending}
                    />
                </div>
            </div>
        </>
    );
}
