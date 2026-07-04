import { z } from 'zod';
import { AUTH_LIMITS } from '@/const/auth.constants';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */
export const loginSchema = z.object({
    email: z.string().trim().min(1).email().max(AUTH_LIMITS.EMAIL_MAX),
    password: z.string().min(1).max(AUTH_LIMITS.PASSWORD_MAX),
});

export const registerSchema = z
    .object({
        firstName: z.string().trim().min(1).max(AUTH_LIMITS.FIRST_NAME_MAX),
        lastName: z.string().trim().min(1).max(AUTH_LIMITS.LAST_NAME_MAX),
        email: z.string().trim().min(1).email().max(AUTH_LIMITS.EMAIL_MAX),
        password: z
            .string()
            .min(AUTH_LIMITS.PASSWORD_MIN)
            .max(AUTH_LIMITS.PASSWORD_MAX)
            // .regex() types in Zod v3 don't expose params — .refine() is the type-safe equivalent
            // Matches at least one uppercase ASCII letter
            .refine((val) => /[A-Z]/.test(val), { params: { i18n: 'custom.password_uppercase' } })
            // Matches at least one lowercase ASCII letter
            .refine((val) => /[a-z]/.test(val), { params: { i18n: 'custom.password_lowercase' } })
            // Matches at least one digit
            .refine((val) => /[0-9]/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: z.string().min(1),
    })
    .refine((data) => data.password === data.confirmPassword, {
        params: { i18n: 'custom.passwords_mismatch' },
        path: ['confirmPassword'],
    });

export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;

export const forgotPasswordSchema = z.object({
    email: z.string().trim().min(1).email().max(AUTH_LIMITS.EMAIL_MAX),
});

export const resetPasswordSchema = z
    .object({
        password: z
            .string()
            .min(AUTH_LIMITS.PASSWORD_MIN)
            .max(AUTH_LIMITS.PASSWORD_MAX)
            // Matches at least one uppercase ASCII letter
            .refine((val) => /[A-Z]/.test(val), { params: { i18n: 'custom.password_uppercase' } })
            // Matches at least one lowercase ASCII letter
            .refine((val) => /[a-z]/.test(val), { params: { i18n: 'custom.password_lowercase' } })
            // Matches at least one digit
            .refine((val) => /[0-9]/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: z.string().min(1),
    })
    .refine((data) => data.password === data.confirmPassword, {
        params: { i18n: 'custom.passwords_mismatch' },
        path: ['confirmPassword'],
    });

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;
export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export const changePasswordSchema = z
    .object({
        currentPassword: z.string().min(1),
        newPassword: z
            .string()
            .min(AUTH_LIMITS.PASSWORD_MIN)
            .max(AUTH_LIMITS.PASSWORD_MAX)
            .refine((val) => /[A-Z]/.test(val), { params: { i18n: 'custom.password_uppercase' } })
            .refine((val) => /[a-z]/.test(val), { params: { i18n: 'custom.password_lowercase' } })
            .refine((val) => /[0-9]/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: z.string().min(1),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
        params: { i18n: 'custom.passwords_mismatch' },
        path: ['confirmPassword'],
    });

export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;

export const setPasswordSchema = z
    .object({
        newPassword: z
            .string()
            .min(AUTH_LIMITS.PASSWORD_MIN)
            .max(AUTH_LIMITS.PASSWORD_MAX)
            .refine((val) => /[A-Z]/.test(val), { params: { i18n: 'custom.password_uppercase' } })
            .refine((val) => /[a-z]/.test(val), { params: { i18n: 'custom.password_lowercase' } })
            .refine((val) => /[0-9]/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: z.string().min(1),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
        params: { i18n: 'custom.passwords_mismatch' },
        path: ['confirmPassword'],
    });

export type SetPasswordFormData = z.infer<typeof setPasswordSchema>;
