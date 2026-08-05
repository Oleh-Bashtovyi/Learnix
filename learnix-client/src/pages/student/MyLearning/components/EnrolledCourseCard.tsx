import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { BookOpen } from 'lucide-react';
import { RatingStars } from '@/components/common/elements/RatingStars';
import { lastLessonStorageKey } from '@/const/lesson.constants';
import { EnrollmentStatus } from '@/enums/enrollment.enums';
import { APP_ROUTES } from '@/routes/paths';
import type { EnrolledCourseDto } from '@/types/enrollment.types';
import { cn } from '@/utils/cn';

interface EnrolledCourseCardProps {
    enrollment: EnrolledCourseDto;
    className?: string;
}

const GRADIENT_FALLBACKS = [
    'from-primary/30 to-accent/30',
    'from-accent/30 to-success/30',
    'from-warning/30 to-primary/30',
    'from-success/30 to-primary/30',
    'from-primary/30 to-warning/30',
    'from-accent/30 to-warning/30',
];

function pickGradient(courseId: string): string {
    const sum = courseId.split('').reduce((acc, ch) => acc + (ch.codePointAt(0) ?? 0), 0);
    return GRADIENT_FALLBACKS[sum % GRADIENT_FALLBACKS.length];
}

export function EnrolledCourseCard({ enrollment, className }: EnrolledCourseCardProps) {
    const { t } = useTranslation('myLearning');
    const [imgFailed, setImgFailed] = useState(false);
    const showImage = !!enrollment.coverImageUrl && !imgFailed;
    const gradientClass = pickGradient(enrollment.courseId);
    const isCompleted = enrollment.enrollmentStatus === EnrollmentStatus.Completed;

    const { completedLessons, totalLessons, myRating } = enrollment;
    const rawPercent =
        totalLessons > 0 ? Math.min(100, Math.round((completedLessons / totalLessons) * 100)) : 0;
    // A completed enrollment always reads as done, even if its lesson counts are missing or stale.
    const percent = isCompleted ? 100 : rawPercent;
    const isFull = percent === 100;
    const notStarted = !isCompleted && completedLessons === 0;

    const lastLessonId = localStorage.getItem(lastLessonStorageKey(enrollment.courseId));
    const destination = lastLessonId
        ? APP_ROUTES.student.learnLesson(enrollment.courseId, lastLessonId)
        : APP_ROUTES.student.learnCourse(enrollment.courseId);
    const reviewHref = APP_ROUTES.public.courseDetailReviews(enrollment.courseId);

    // The whole card is the link, but the <a> only wraps the title: a stretched pseudo-element covers
    // the card for the mouse, while the keyboard gets one real link.
    return (
        <div
            className={cn(
                'group relative flex flex-col overflow-hidden rounded-xl border border-border bg-card transition-all',
                'hover:-translate-y-1 hover:shadow-xl',
                className,
            )}
        >
            <div
                className={cn(
                    'relative aspect-video bg-gradient-to-br',
                    showImage ? '' : gradientClass,
                )}
            >
                {showImage ? (
                    <img
                        src={enrollment.coverImageUrl!}
                        alt=""
                        className="absolute inset-0 size-full object-cover"
                        onError={() => setImgFailed(true)}
                    />
                ) : (
                    <div className="absolute inset-0 flex items-center justify-center">
                        <BookOpen className="size-10 text-white/40" />
                    </div>
                )}
            </div>

            <div className="flex flex-1 flex-col p-5">
                {/* Reserve two lines so a one-line title doesn't pull the bar up: with the bar bottom-
                    anchored, every card's footer then lands at the same height. */}
                <h3 className="line-clamp-2 min-h-[2lh] font-heading text-base font-semibold group-hover:text-primary">
                    <Link
                        to={destination}
                        className="after:absolute after:inset-0 after:content-[''] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    >
                        {enrollment.courseTitle}
                    </Link>
                </h3>

                {enrollment.instructorName && (
                    <p className="mt-1 line-clamp-1 text-xs text-muted-foreground">
                        {enrollment.instructorName}
                    </p>
                )}

                {/* Progress + rating — the bar replaces the status badge: it says both "how far" and,
                    at 100%, "done". Certificates live in the Certificates tab and the course player,
                    so they are deliberately absent here. */}
                <div className="mt-auto pt-4">
                    <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                        <div
                            className={cn(
                                'h-full rounded-full transition-all',
                                isFull ? 'bg-success' : 'bg-primary',
                            )}
                            style={{ width: `${percent}%` }}
                        />
                    </div>

                    {/* min-h reserves two text rows so a single-line variant (e.g. "Start course" /
                        "Leave a rating") keeps the bar at the same height as a two-line card. */}
                    <div className="mt-2 flex min-h-[2lh] items-start justify-between gap-3 text-xs">
                        <div className="min-w-0">
                            {notStarted ? (
                                <span className="font-medium text-primary">{t('startCourse')}</span>
                            ) : (
                                <>
                                    <span className="block font-medium text-foreground">
                                        {t('percentComplete', { percent })}
                                    </span>
                                    <span className="block text-muted-foreground">
                                        {t('lessonsProgress', {
                                            completed: completedLessons,
                                            total: totalLessons,
                                        })}
                                    </span>
                                </>
                            )}
                        </div>

                        {myRating != null ? (
                            <div className="shrink-0 text-right">
                                <RatingStars value={myRating} size="sm" className="justify-end" />
                                <span className="mt-0.5 block text-xs text-muted-foreground">
                                    {t('yourRating')}
                                </span>
                            </div>
                        ) : (
                            // Reviewing is gated behind finishing a lesson (matches the backend rule), so
                            // the prompt only appears once the student has actually started the course.
                            // z-10 lifts the link above the title's stretched overlay so it stays clickable.
                            completedLessons >= 1 && (
                                <Link
                                    to={reviewHref}
                                    className="relative z-10 shrink-0 text-xs font-medium text-primary hover:underline"
                                >
                                    {t('leaveRating')}
                                </Link>
                            )
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
