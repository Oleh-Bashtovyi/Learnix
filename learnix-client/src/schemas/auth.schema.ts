import { z } from 'zod';
import { AUTH_LIMITS } from '@/const/auth.constants';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */

// `.refine()`, not `.min(1, { message: '...' })` — a message passed directly to a check
// short-circuits Zod's error-map resolution entirely (see `makeIssue` in zod's `parseUtil`), so the
// key would render as the literal string instead of being looked up as a translation. `.refine()`
// raises a `custom` issue instead, the one code `zod-i18n-map` resolves through `issue.params.i18n`
// — the same mechanism already used below for `password_uppercase` / `password_digit` etc.
const requiredString = (i18nKey: string) =>
    z.string().refine((val) => val.length > 0, { params: { i18n: i18nKey } });

const requiredTrimmedString = (i18nKey: string) =>
    z
        .string()
        .trim()
        .refine((val) => val.length > 0, { params: { i18n: i18nKey } });

export const loginSchema = z.object({
    email: requiredTrimmedString('custom.required_field').pipe(
        z.string().email().max(AUTH_LIMITS.EMAIL_MAX),
    ),
    password: requiredString('custom.required_field').pipe(
        z.string().max(AUTH_LIMITS.PASSWORD_MAX),
    ),
});

export const registerSchema = z
    .object({
        firstName: requiredTrimmedString('custom.required_field').pipe(
            z.string().max(AUTH_LIMITS.FIRST_NAME_MAX),
        ),
        lastName: requiredTrimmedString('custom.required_field').pipe(
            z.string().max(AUTH_LIMITS.LAST_NAME_MAX),
        ),
        email: requiredTrimmedString('custom.required_field').pipe(
            z.string().email().max(AUTH_LIMITS.EMAIL_MAX),
        ),
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
            .refine((val) => /\d/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: requiredString('custom.confirm_password_required'),
    })
    .refine((data) => data.password === data.confirmPassword, {
        params: { i18n: 'custom.passwords_mismatch' },
        path: ['confirmPassword'],
    });

export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;

export const forgotPasswordSchema = z.object({
    email: requiredTrimmedString('custom.required_field').pipe(
        z.string().email().max(AUTH_LIMITS.EMAIL_MAX),
    ),
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
            .refine((val) => /\d/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: requiredString('custom.confirm_password_required'),
    })
    .refine((data) => data.password === data.confirmPassword, {
        params: { i18n: 'custom.passwords_mismatch' },
        path: ['confirmPassword'],
    });

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;
export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export const changePasswordSchema = z
    .object({
        currentPassword: requiredString('custom.required_field'),
        newPassword: z
            .string()
            .min(AUTH_LIMITS.PASSWORD_MIN)
            .max(AUTH_LIMITS.PASSWORD_MAX)
            .refine((val) => /[A-Z]/.test(val), { params: { i18n: 'custom.password_uppercase' } })
            .refine((val) => /[a-z]/.test(val), { params: { i18n: 'custom.password_lowercase' } })
            .refine((val) => /\d/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: requiredString('custom.confirm_password_required'),
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
            .refine((val) => /\d/.test(val), { params: { i18n: 'custom.password_digit' } }),
        confirmPassword: requiredString('custom.confirm_password_required'),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
        params: { i18n: 'custom.passwords_mismatch' },
        path: ['confirmPassword'],
    });

export type SetPasswordFormData = z.infer<typeof setPasswordSchema>;
