import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { HeartOff } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { WishlistCourseDto } from '@/types/wishlist.types';
import { cn } from '@/utils/cn';
import { wishlistApi } from '@/api/wishlist.api';
import { queryKeys } from '@/api/queryKeys';

interface WishlistCardProps {
    course: WishlistCourseDto;
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

export function WishlistCard({ course, className }: WishlistCardProps) {
    const { t } = useTranslation('wishlist');
    const gradientClass = pickGradient(course.courseId);
    const [imgFailed, setImgFailed] = useState(false);
    const showImage = !!course.coverImageUrl && !imgFailed;
    const queryClient = useQueryClient();

    const removeMutation = useMutation({
        mutationFn: wishlistApi.remove,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.wishlist.mine() });
            toast.success(t('removedSuccess'));
        },
        onError: () => {
            toast.error(t('removedError'));
        },
    });

    const handleRemove = (e: React.MouseEvent) => {
        e.preventDefault(); // Prevent navigating to course page
        e.stopPropagation();
        removeMutation.mutate(course.courseId);
    };

    return (
        <Link
            to={`/courses/${course.courseId}`}
            className={cn(
                'group relative block overflow-hidden rounded-xl border border-border bg-card transition-all',
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
                {course.isFree && (
                    <span className="absolute left-3 top-3 rounded bg-success px-2 py-1 text-xs font-medium text-white">
                        {t('free').toUpperCase()}
                    </span>
                )}

                {/* Remove button wrapper */}
                <button
                    type="button"
                    onClick={handleRemove}
                    disabled={removeMutation.isPending}
                    className="absolute right-3 top-3 flex h-8 w-8 items-center justify-center rounded-full bg-background/80 backdrop-blur transition-colors hover:bg-destructive hover:text-white disabled:opacity-50"
                    title={t('remove')}
                >
                    <HeartOff className="h-4 w-4" />
                </button>
            </div>

            <div className="p-5">
                <h3 className="mt-1 line-clamp-2 font-heading text-lg font-semibold group-hover:text-primary">
                    {course.title}
                </h3>

                <div className="mt-4 flex items-center justify-between border-t border-border pt-4">
                    <span className="text-xs text-muted-foreground">
                        {t('addedOn')} {new Date(course.addedAt).toLocaleDateString()}
                    </span>
                    <span className={cn('font-heading font-bold', course.isFree && 'text-success')}>
                        {course.isFree ? t('free') : `$${course.price}`}
                    </span>
                </div>
            </div>
        </Link>
    );
}
