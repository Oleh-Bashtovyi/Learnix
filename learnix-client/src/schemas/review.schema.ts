import { z } from 'zod';
import { REVIEW_LIMITS } from '@/const/review.constants';

export const reviewSchema = z.object({
    rating: z
        .number()
        .min(REVIEW_LIMITS.RATING_MIN, 'Rating is required')
        .max(REVIEW_LIMITS.RATING_MAX),
    comment: z
        .string()
        .max(
            REVIEW_LIMITS.COMMENT_MAX,
            `Comment must be ${REVIEW_LIMITS.COMMENT_MAX} characters or less`,
        )
        .optional(),
});

export type ReviewFormValues = z.infer<typeof reviewSchema>;
