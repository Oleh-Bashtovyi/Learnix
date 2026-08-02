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
    // .refine(), not .min(1, { message }) — a message passed directly to a check short-circuits
    // Zod's error-map resolution entirely (see makeIssue in zod's parseUtil), so the string would be
    // rendered as-is instead of being looked up as a translation key. .refine() raises a `custom`
    // issue instead, which is the one code zod-i18n-map resolves through `issue.params.i18n`.
    categoryId: z
        .string()
        .trim()
        .refine((val) => val.length > 0, { params: { i18n: 'custom.required_field' } }),
    price: z.number().min(COURSE_LIMITS.PRICE_MIN),
    coverImageUrl: z.string().nullable().optional(),
    tags: z
        .array(z.string().trim().min(1).max(COURSE_LIMITS.TAG_MAX_LENGTH))
        .max(COURSE_LIMITS.TAGS_MAX_COUNT),
});

export type CourseInfoFormData = z.infer<typeof courseInfoSchema>;
