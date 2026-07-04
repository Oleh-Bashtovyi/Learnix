import { z } from 'zod';
import { COURSE_LIMITS } from '@/const/course.constants';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */
export const courseInfoSchema = z.object({
    title: z.string().trim().min(COURSE_LIMITS.TITLE_MIN).max(COURSE_LIMITS.TITLE_MAX),
    description: z
        .string()
        .trim()
        .min(COURSE_LIMITS.DESCRIPTION_MIN)
        .max(COURSE_LIMITS.DESCRIPTION_MAX),
    categoryId: z.string().trim().min(1),
    price: z.number().min(COURSE_LIMITS.PRICE_MIN),
    coverImageUrl: z.string().nullable().optional(),
    tags: z
        .array(z.string().trim().min(1).max(COURSE_LIMITS.TAG_MAX_LENGTH))
        .max(COURSE_LIMITS.TAGS_MAX_COUNT),
});

export type CourseInfoFormData = z.infer<typeof courseInfoSchema>;
