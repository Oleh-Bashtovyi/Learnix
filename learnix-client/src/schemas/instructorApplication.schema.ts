import { z } from 'zod';
import { INSTRUCTOR_APP_LIMITS } from '@/const/instructor.constants';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */
export const instructorApplicationSchema = z.object({
    motivationText: z
        .string()
        .min(INSTRUCTOR_APP_LIMITS.MOTIVATION_MIN)
        .max(INSTRUCTOR_APP_LIMITS.MOTIVATION_MAX),
    portfolioUrl: z
        .string()
        .url()
        .max(INSTRUCTOR_APP_LIMITS.PORTFOLIO_URL_MAX)
        .optional()
        .or(z.literal('')),
});

export type InstructorApplicationFormData = z.infer<typeof instructorApplicationSchema>;
