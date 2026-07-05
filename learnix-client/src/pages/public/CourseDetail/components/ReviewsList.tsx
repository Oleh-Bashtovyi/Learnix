import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { User } from 'lucide-react';
import { RatingStars } from '@/components/common/ui/RatingStars';
import type { CourseReviewDto } from '@/types/review.types';
import { cn } from '@/utils/cn';

interface ReviewsListProps {
    reviews: CourseReviewDto[];
    averageRating: number;
    totalCount: number;
}

type ReviewItemProps = {
    review: CourseReviewDto;
};

function ReviewItem({ review }: ReviewItemProps) {
    const { t } = useTranslation('courseDetail');
    const [isExpanded, setIsExpanded] = useState(false);
    const [isExpandable, setIsExpandable] = useState(false);
    const textRef = useRef<HTMLParagraphElement>(null);

    useEffect(() => {
        const checkTruncation = () => {
            if (textRef.current && !isExpanded) {
                setIsExpandable(textRef.current.scrollHeight > textRef.current.clientHeight);
            }
        };

        checkTruncation();
        window.addEventListener('resize', checkTruncation);
        return () => window.removeEventListener('resize', checkTruncation);
    }, [review.comment, isExpanded]);

    return (
        <div className="rounded-xl border border-border bg-card p-5">
            <div className="flex items-start gap-4">
                <div className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-muted">
                    {review.studentAvatarBlobPath ? (
                        <img
                            src={review.studentAvatarBlobPath}
                            alt={review.studentFirstName}
                            className="size-full object-cover"
                        />
                    ) : (
                        <User className="size-5 text-muted-foreground" />
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
                </div>
            </div>

            {review.comment && (
                <div className="mt-3">
                    <p
                        ref={textRef}
                        className={cn(
                            'whitespace-pre-wrap break-words text-sm text-foreground',
                            !isExpanded && 'line-clamp-6',
                        )}
                    >
                        {review.comment}
                    </p>
                    {isExpandable && (
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
