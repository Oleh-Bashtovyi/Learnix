import { useTranslation } from 'react-i18next';
import { Link, useLocation } from 'react-router-dom';
import { BookOpen, Heart } from 'lucide-react';
import { APP_ROUTES } from '@/routes/paths';
import type { UserSummary } from '@/store/auth.store';
import type { CourseDetailDto } from '@/types/course.types';
import { cn } from '@/utils/cn';

interface CourseSidebarProps {
    course: CourseDetailDto;
    isFree: boolean;
    isOwnCourse: boolean;
    isEnrolled: boolean;
    user: UserSummary | null;
    inWishlist: boolean;
    enrollIsPending: boolean;
    onEnroll: () => void;
    onToggleWishlist: () => void;
    wishlistIsPending: boolean;
}

export function CourseSidebar({
    course,
    isFree,
    isOwnCourse,
    isEnrolled,
    user,
    inWishlist,
    enrollIsPending,
    onEnroll,
    onToggleWishlist,
    wishlistIsPending,
}: CourseSidebarProps) {
    const { t } = useTranslation('courseDetail');
    const location = useLocation();

    return (
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
                        <BookOpen className="size-12 text-muted-foreground/40" />
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
                        to={
                            course.sections[0]?.lessons[0]
                                ? APP_ROUTES.student.learnLesson(
                                      course.id,
                                      course.sections[0].lessons[0].id,
                                  )
                                : APP_ROUTES.student.learnCourse(course.id)
                        }
                        className="mt-4 flex w-full items-center justify-center rounded-lg bg-success px-4 py-3 font-medium text-white transition-opacity hover:opacity-90"
                    >
                        {t('enroll.enrolled')}
                    </Link>
                ) : user ? (
                    <button
                        type="button"
                        onClick={onEnroll}
                        disabled={enrollIsPending}
                        className="mt-4 w-full rounded-lg bg-primary px-4 py-3 font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
                    >
                        {enrollIsPending
                            ? t('enroll.enrolling')
                            : isFree
                              ? t('enroll.free')
                              : t('enroll.paid', { price: course.price })}
                    </button>
                ) : (
                    <Link
                        to={APP_ROUTES.public.login}
                        state={{ from: location }}
                        className="mt-4 flex w-full items-center justify-center rounded-lg bg-primary px-4 py-3 font-medium text-primary-foreground transition-opacity hover:opacity-90"
                    >
                        {t('enroll.loginRequired')}
                    </Link>
                )}

                {/* Wishlist toggle */}
                {user && !isEnrolled && !isOwnCourse && (
                    <button
                        type="button"
                        onClick={onToggleWishlist}
                        disabled={wishlistIsPending}
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
                        {wishlistIsPending
                            ? inWishlist
                                ? t('wishlist.removing')
                                : t('wishlist.saving')
                            : inWishlist
                              ? t('wishlist.saved')
                              : t('wishlist.save')}
                    </button>
                )}

                {/* Instructor info */}
                <div className="mt-5 border-t border-border pt-4 text-sm text-muted-foreground">
                    <p>
                        <span className="font-medium text-foreground">{t('instructor.label')}</span>
                        {course.instructorFullName && (
                            <Link
                                to={APP_ROUTES.public.instructorProfile(course.instructorId)}
                                className="ml-1 text-primary hover:underline"
                            >
                                {course.instructorFullName}
                            </Link>
                        )}
                    </p>
                </div>
            </div>
        </aside>
    );
}
