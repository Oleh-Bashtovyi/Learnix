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
                        'relative aspect-video bg-gradient-to-br',
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

                <CardContent className="flex flex-1 flex-col p-5">
                    <span className="text-xs font-medium text-accent">{course.categoryName}</span>
                    <h3 className="mt-1 font-heading text-lg font-semibold group-hover:text-primary">
                        {course.title}
                    </h3>
                    <p className="mt-2 line-clamp-2 text-sm text-muted-foreground">
                        {course.description}
                    </p>

                    <div className="mt-3 flex items-center gap-3 pb-4 text-xs text-muted-foreground">
                        <div className="flex items-center gap-2">
                            <Avatar className="size-5">
                                <AvatarFallback className="text-[10px]">
                                    {course.instructor.fullName.charAt(0)}
                                </AvatarFallback>
                            </Avatar>
                            <span>{course.instructor.fullName}</span>
                        </div>
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
                </CardContent>
            </Link>
        </Card>
    );
}
