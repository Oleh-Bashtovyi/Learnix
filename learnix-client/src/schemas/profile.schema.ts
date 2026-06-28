import { z } from 'zod';
import { PROFILE_LIMITS } from '@/const/profile.constants';

export const profileSchema = z.object({
    firstName: z
        .string()
        .trim()
        .min(1, 'First name is required')
        .max(PROFILE_LIMITS.FIRST_NAME_MAX),
    lastName: z.string().trim().min(1, 'Last name is required').max(PROFILE_LIMITS.LAST_NAME_MAX),
    bio: z
        .string()
        .trim()
        .max(PROFILE_LIMITS.BIO_MAX, `Bio must be ${PROFILE_LIMITS.BIO_MAX} characters or less`)
        .optional(),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;
