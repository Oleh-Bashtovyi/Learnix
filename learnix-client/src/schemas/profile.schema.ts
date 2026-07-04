import { z } from 'zod';
import { PROFILE_LIMITS } from '@/const/profile.constants';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */
export const profileSchema = z.object({
    firstName: z.string().trim().min(1).max(PROFILE_LIMITS.FIRST_NAME_MAX),
    lastName: z.string().trim().min(1).max(PROFILE_LIMITS.LAST_NAME_MAX),
    bio: z.string().trim().max(PROFILE_LIMITS.BIO_MAX).optional(),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;
