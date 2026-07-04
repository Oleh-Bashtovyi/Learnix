import { z } from 'zod';
import { REVIEW_LIMITS } from '@/const/review.constants';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */
export const reviewSchema = z.object({
    rating: z.number().min(REVIEW_LIMITS.RATING_MIN).max(REVIEW_LIMITS.RATING_MAX),
    comment: z.string().max(REVIEW_LIMITS.COMMENT_MAX).optional(),
});

export type ReviewFormValues = z.infer<typeof reviewSchema>;
