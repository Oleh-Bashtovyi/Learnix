import { useTranslation } from 'react-i18next';
import { RatingStars } from '@/components/common/elements/RatingStars';
import type { InstructorRecentReview } from '@/types/instructorAnalytics.types';
import { ChartCard } from './ChartCard';

interface RecentReviewsListProps {
    data?: InstructorRecentReview[];
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    className?: string;
    /** Every row already shares one course once the analytics page is filtered down to it — repeating
        the title on each review would only be noise. Defaults to showing it (the all-courses view). */
    showCourseTitle?: boolean;
}

export function RecentReviewsList({
    data,
    isLoading,
    isError,
    onRetry,
    className,
    showCourseTitle = true,
}: RecentReviewsListProps) {
    const { t } = useTranslation('instructorAnalytics');

    return (
        <ChartCard
            title={t('recentReviews.title')}
            className={className}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={!data || data.length === 0}
        >
            <ul className="max-h-96 divide-y divide-border overflow-y-auto overscroll-contain pr-1">
                {data?.map((review, i) => (
                    <li key={`${review.courseId}-${i}`} className="py-3 first:pt-0 last:pb-0">
                        <div className="flex items-center justify-between gap-3">
                            <div className="min-w-0">
                                <p className="truncate text-sm font-medium text-foreground">
                                    {review.studentName}
                                </p>
                                {showCourseTitle && (
                                    <p className="truncate text-xs text-muted-foreground">
                                        {review.courseTitle}
                                    </p>
                                )}
                            </div>
                            <div className="flex shrink-0 flex-col items-end gap-1">
                                <RatingStars value={review.rating} size="sm" />
                                <span className="text-xs text-muted-foreground">
                                    {new Date(review.createdAt).toLocaleDateString()}
                                </span>
                            </div>
                        </div>
                        {review.text && (
                            <p className="mt-1.5 line-clamp-2 text-sm text-muted-foreground">
                                {review.text}
                            </p>
                        )}
                    </li>
                ))}
            </ul>
        </ChartCard>
    );
}
