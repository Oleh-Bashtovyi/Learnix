import { User } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { cn } from '@/utils/cn';
import { RatingStars } from '@/components/common/RatingStars';
import type { CourseReviewDto } from '@/types/review.types';

interface ReviewsListProps {
    reviews: CourseReviewDto[];
    averageRating: number;
    totalCount: number;
}

function ReviewItem({ review }: { review: CourseReviewDto }) {
    const { t } = useTranslation('courseDetail');
    const [isExpanded, setIsExpanded] = useState(false);

    // Heuristic for showing the 'Read more' toggle:
    // If the text has more than 250 characters or more than 4 newlines
    const isLong =
        review.comment &&
        (review.comment.length > 250 || (review.comment.match(/\n/g) || []).length > 4);

    return (
        <div className="rounded-xl border border-border bg-card p-5">
            <div className="flex items-start gap-4">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-muted">
                    {review.studentAvatarBlobPath ? (
                        <img
                            src={review.studentAvatarBlobPath}
                            alt={review.studentFirstName}
                            className="h-full w-full object-cover"
                        />
                    ) : (
                        <User className="h-5 w-5 text-muted-foreground" />
                    )}
                </div>

                <div className="flex-1">
                    <div className="flex items-center justify-between">
                        <p className="font-medium text-foreground">
                            {review.studentFirstName} {review.studentLastName}
                        </p>
                        <span className="text-xs text-muted-foreground">
                            {new Date(review.createdAt).toLocaleDateString('en-US', {
                                month: 'short',
                                day: 'numeric',
                                year: 'numeric',
                            })}
                        </span>
                    </div>

                    <RatingStars value={review.rating} size="sm" className="mt-1" />

                    {review.comment && (
                        <div className="mt-2">
                            <p
                                className={cn(
                                    'whitespace-pre-wrap break-words text-sm text-foreground',
                                    !isExpanded && 'line-clamp-6',
                                )}
                            >
                                {review.comment}
                            </p>
                            {isLong && (
                                <button
                                    onClick={() => setIsExpanded(!isExpanded)}
                                    className="mt-1 text-sm font-medium text-primary hover:underline"
                                >
                                    {isExpanded ? t('reviews.showLess') : t('reviews.showMore')}
                                </button>
                            )}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export function ReviewsList({ reviews, averageRating, totalCount }: ReviewsListProps) {
    const { t } = useTranslation('courseDetail');

    return (
        <section className="space-y-4">
            <div className="flex items-baseline justify-between">
                <h2 className="font-heading text-xl font-semibold text-foreground">
                    {t('reviews.title')}
                </h2>
                {totalCount > 0 && (
                    <div className="flex items-center gap-2">
                        <RatingStars value={averageRating} size="sm" />
                        <span className="text-sm font-medium">{averageRating.toFixed(1)}</span>
                        <span className="text-sm text-muted-foreground">
                            ({t('reviews.reviewCount', { count: totalCount })})
                        </span>
                    </div>
                )}
            </div>

            {reviews.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t('reviews.noReviews')}</p>
            ) : (
                <div className="space-y-4">
                    {reviews.map((review) => (
                        <ReviewItem key={review.id} review={review} />
                    ))}
                </div>
            )}
        </section>
    );
}
