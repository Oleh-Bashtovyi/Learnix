import { useState } from 'react';
import { Link } from 'react-router-dom';
import { BookOpen } from 'lucide-react';

import type { EnrolledCourseDto } from '@/types/enrollment.types';
import { cn } from '@/utils/cn';
import { MY_LEARNING } from '@/const/localization/myLearning';

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
    const sum = courseId.split('').reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
    return GRADIENT_FALLBACKS[sum % GRADIENT_FALLBACKS.length];
}

export function EnrolledCourseCard({ enrollment, className }: EnrolledCourseCardProps) {
    const [imgFailed, setImgFailed] = useState(false);
    const showImage = !!enrollment.coverImageUrl && !imgFailed;
    const gradientClass = pickGradient(enrollment.courseId);
    const isCompleted = enrollment.enrollmentStatus === 'Completed';

    const lastLessonId = localStorage.getItem(`lastLesson_${enrollment.courseId}`);
    const destination = lastLessonId
        ? `/courses/${enrollment.courseId}/learn/${lastLessonId}`
        : `/courses/${enrollment.courseId}/learn`;

    return (
        <Link
            to={destination}
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
                {showImage ? (
                    <img
                        src={enrollment.coverImageUrl!}
                        alt=""
                        className="absolute inset-0 h-full w-full object-cover"
                        onError={() => setImgFailed(true)}
                    />
                ) : (
                    <div className="absolute inset-0 flex items-center justify-center">
                        <BookOpen className="h-10 w-10 text-white/40" />
                    </div>
                )}

                <span
                    className={cn(
                        'absolute left-3 top-3 rounded px-2 py-1 text-xs font-medium text-white',
                        isCompleted ? 'bg-success' : 'bg-primary',
                    )}
                >
                    {isCompleted ? MY_LEARNING.statusCompleted : MY_LEARNING.statusActive}
                </span>
            </div>

            <div className="flex flex-1 flex-col p-5">
                <h3 className="line-clamp-2 font-heading text-base font-semibold group-hover:text-primary">
                    {enrollment.courseTitle}
                </h3>

                <div className="mt-auto space-y-1 pt-4 text-xs text-muted-foreground">
                    <p>
                        {MY_LEARNING.enrolledOn}{' '}
                        {new Date(enrollment.enrolledAt).toLocaleDateString()}
                    </p>
                    {isCompleted && enrollment.completedAt && (
                        <p>
                            {MY_LEARNING.completedOn}{' '}
                            {new Date(enrollment.completedAt).toLocaleDateString()}
                        </p>
                    )}
                </div>

                <div className="mt-4 flex items-center justify-end border-t border-border pt-4">
                    <span className="text-sm font-medium text-primary">
                        {MY_LEARNING.continueLearning} →
                    </span>
                </div>
            </div>
        </Link>
    );
}
