import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { BookOpen } from 'lucide-react';
import { CourseCertificateButton } from '@/components/common/course/CourseCertificateButton';
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
    const sum = courseId.split('').reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
    return GRADIENT_FALLBACKS[sum % GRADIENT_FALLBACKS.length];
}

export function EnrolledCourseCard({ enrollment, className }: EnrolledCourseCardProps) {
    const navigate = useNavigate();
    const { t } = useTranslation('myLearning');
    const [imgFailed, setImgFailed] = useState(false);
    const showImage = !!enrollment.coverImageUrl && !imgFailed;
    const gradientClass = pickGradient(enrollment.courseId);
    const isCompleted = enrollment.enrollmentStatus === 'Completed';

    const lastLessonId = localStorage.getItem(`lastLesson_${enrollment.courseId}`);
    const destination = lastLessonId
        ? `/courses/${enrollment.courseId}/learn/${lastLessonId}`
        : `/courses/${enrollment.courseId}/learn`;

    return (
        <div
            onClick={() => navigate(destination)}
            className={cn(
                'group flex cursor-pointer flex-col overflow-hidden rounded-xl border border-border bg-card transition-all',
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

                <span
                    className={cn(
                        'absolute left-3 top-3 rounded px-2 py-1 text-xs font-medium',
                        isCompleted ? 'bg-success text-white' : 'bg-brand text-brand-foreground',
                    )}
                >
                    {isCompleted ? t('common:status.completed') : t('statusActive')}
                </span>
            </div>

            <div className="flex flex-1 flex-col p-5">
                <h3 className="line-clamp-2 font-heading text-base font-semibold group-hover:text-primary">
                    {enrollment.courseTitle}
                </h3>

                <div className="mt-2 space-y-1 text-xs text-muted-foreground">
                    <p>
                        {t('enrolledOn')} {new Date(enrollment.enrolledAt).toLocaleDateString()}
                    </p>
                    {isCompleted && enrollment.completedAt && (
                        <p>
                            {t('common:status.completed')}{' '}
                            {new Date(enrollment.completedAt).toLocaleDateString()}
                        </p>
                    )}
                </div>

                {isCompleted && (
                    <div className="mt-auto flex flex-wrap items-center gap-3 pt-4">
                        <div onClick={(e) => e.stopPropagation()}>
                            <CourseCertificateButton
                                courseId={enrollment.courseId}
                                variant="outline"
                                className="h-9 py-0"
                            />
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
