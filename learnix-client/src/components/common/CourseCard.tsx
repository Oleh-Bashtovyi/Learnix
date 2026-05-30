import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { CourseSummaryDto } from '@/types/course.types';
import { cn } from '@/utils/cn';

interface CourseCardProps {
    course: CourseSummaryDto;
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
    const sum = courseId.split('').reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
    return GRADIENT_FALLBACKS[sum % GRADIENT_FALLBACKS.length];
}

function formatPrice(price: number): string {
    return price === 0 ? 'Free' : `$${price}`;
}

function formatReviewsCount(count: number): string {
    return count >= 1000 ? `${(count / 1000).toFixed(1)}k` : `${count}`;
}

export function CourseCard({ course, className }: CourseCardProps) {
    const gradientClass = pickGradient(course.id);
    const isFree = course.price === 0;
    const [imgFailed, setImgFailed] = useState(false);
    const showImage = !!course.coverImageUrl && !imgFailed;

    return (
        <Link
            to={`/courses/${course.id}`}
            className={cn(
                'group flex flex-col overflow-hidden rounded-xl border border-border bg-card transition-all',
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
                {showImage && (
                    <img
                        src={course.coverImageUrl!}
                        alt=""
                        className="absolute inset-0 h-full w-full object-cover"
                        onError={() => setImgFailed(true)}
                    />
                )}
                {course.badge === 'bestseller' && (
                    <span className="absolute left-3 top-3 rounded bg-card/90 px-2 py-1 text-xs font-medium backdrop-blur">
                        ⭐ Bestseller
                    </span>
                )}
                {course.badge === 'new' && (
                    <span className="absolute left-3 top-3 rounded bg-accent px-2 py-1 text-xs font-medium text-white">
                        NEW
                    </span>
                )}
                {isFree && !course.badge && (
                    <span className="absolute left-3 top-3 rounded bg-success px-2 py-1 text-xs font-medium text-white">
                        FREE
                    </span>
                )}
            </div>

            <div className="flex flex-1 flex-col p-5">
                <span className="text-xs font-medium text-accent">{course.categoryName}</span>
                <h3 className="mt-1 font-heading text-lg font-semibold group-hover:text-primary">
                    {course.title}
                </h3>
                <p className="mt-2 line-clamp-2 text-sm text-muted-foreground">
                    {course.description}
                </p>

                <div className="mt-3 flex items-center gap-3 pb-4 text-xs text-muted-foreground">
                    <span>👤 {course.instructor.fullName}</span>
                    {course.durationHours > 0 && (
                        <>
                            <span>·</span>
                            <span>{course.durationHours}h video</span>
                        </>
                    )}
                </div>

                <div className="mt-auto flex items-center justify-between border-t border-border pt-4">
                    <div className="flex items-center gap-1 text-sm">
                        <span className="text-warning">★</span>
                        <span className="font-medium">{course.rating.toFixed(1)}</span>
                        <span className="text-muted-foreground">
                            ({formatReviewsCount(course.reviewsCount)})
                        </span>
                    </div>
                    <span className={cn('font-heading font-bold', isFree && 'text-success')}>
                        {formatPrice(course.price)}
                    </span>
                </div>
            </div>
        </Link>
    );
}
