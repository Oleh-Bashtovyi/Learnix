import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { APP_ROUTES } from '@/routes/paths';
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
    const location = useLocation();
    const gradientClass = pickGradient(course.id);
    const isFree = course.price === 0;
    const [imgFailed, setImgFailed] = useState(false);
    const showImage = !!course.coverImageUrl && !imgFailed;

    return (
        <Card
            className={cn(
                'group flex flex-col overflow-hidden transition-all hover:-translate-y-1 hover:shadow-xl',
                className,
            )}
        >
            <Link
                to={APP_ROUTES.public.courseDetail(course.id)}
                state={{ from: `${location.pathname}${location.search}` }}
                className="flex h-full flex-col"
            >
                <div
                    className={cn(
                        'relative aspect-[2/1] bg-gradient-to-br sm:aspect-video',
                        showImage ? '' : gradientClass,
                    )}
                >
                    {showImage && (
                        <img
                            src={course.coverImageUrl!}
                            alt=""
                            className="absolute inset-0 size-full object-cover"
                            onError={() => setImgFailed(true)}
                        />
                    )}
                    {course.badge === 'bestseller' && (
                        <Badge className="absolute left-3 top-3 border-0 bg-yellow-400 font-bold text-yellow-950 shadow-sm hover:bg-yellow-500">
                            Bestseller
                        </Badge>
                    )}
                    {course.badge === 'new' && <Badge className="absolute left-3 top-3">NEW</Badge>}
                </div>

                <CardContent className="flex flex-1 flex-col p-4 sm:p-5">
                    <span className="w-fit rounded-md bg-accent/10 px-2 py-0.5 text-[10px] font-semibold text-accent sm:text-xs">
                        {course.categoryName}
                    </span>
                    <h3 className="mt-2 font-heading text-base font-semibold group-hover:text-primary sm:text-lg">
                        {course.title}
                    </h3>
                    <p className="mt-1.5 line-clamp-2 text-xs text-foreground/80 sm:mt-2 sm:text-sm">
                        {course.description}
                    </p>

                    <div className="mt-3 flex items-center gap-2 pb-3 text-[11px] text-muted-foreground sm:gap-3 sm:pb-4 sm:text-xs">
                        <div className="flex items-center gap-1.5 sm:gap-2">
                            <Avatar className="size-6 sm:size-7">
                                <AvatarFallback className="text-[10px] sm:text-[12px]">
                                    {course.instructor.fullName.charAt(0)}
                                </AvatarFallback>
                            </Avatar>
                            <span className="line-clamp-1">{course.instructor.fullName}</span>
                        </div>
                        {course.durationHours > 0 && (
                            <>
                                <span>·</span>
                                <span className="shrink-0">{course.durationHours}h video</span>
                            </>
                        )}
                    </div>

                    <div className="mt-auto flex items-center justify-between border-t border-border pt-3 sm:pt-4">
                        <div className="flex items-center gap-1 text-xs sm:text-sm">
                            <span className="text-warning">★</span>
                            <span className="font-medium">{course.rating.toFixed(1)}</span>
                            <span className="text-muted-foreground">
                                ({formatReviewsCount(course.reviewsCount)})
                            </span>
                        </div>
                        <span
                            className={cn(
                                'font-heading text-sm font-bold sm:text-base',
                                isFree && 'text-success',
                            )}
                        >
                            {formatPrice(course.price)}
                        </span>
                    </div>
                </CardContent>
            </Link>
        </Card>
    );
}
