import { z } from 'zod';
import { AUTH_LIMITS } from '@/const/auth.constants';

export const loginSchema = z.object({
    email: z
        .string()
        .trim()
        .min(1, 'Email is required')
        .email('Please enter a valid email address')
        .max(AUTH_LIMITS.EMAIL_MAX, 'Email is too long'),
    password: z
        .string()
        .min(1, 'Password is required')
        .max(AUTH_LIMITS.PASSWORD_MAX, 'Password is too long'),
});

export const registerSchema = z
    .object({
        firstName: z
            .string()
            .trim()
            .min(1, 'First name is required')
            .max(AUTH_LIMITS.FIRST_NAME_MAX, 'First name is too long'),
        lastName: z
            .string()
            .trim()
            .min(1, 'Last name is required')
            .max(AUTH_LIMITS.LAST_NAME_MAX, 'Last name is too long'),
        email: z
            .string()
            .trim()
            .min(1, 'Email is required')
            .email('Please enter a valid email address')
            .max(AUTH_LIMITS.EMAIL_MAX, 'Email is too long'),
        password: z
            .string()
            .min(
                AUTH_LIMITS.PASSWORD_MIN,
                `Password must be at least ${AUTH_LIMITS.PASSWORD_MIN} characters`,
            )
            .max(AUTH_LIMITS.PASSWORD_MAX, 'Password is too long')
            .regex(/[A-Z]/, 'Must contain at least one uppercase letter')
            .regex(/[a-z]/, 'Must contain at least one lowercase letter')
            .regex(/[0-9]/, 'Must contain at least one digit'),
        confirmPassword: z.string().min(1, 'Please confirm your password'),
    })
    .refine((data) => data.password === data.confirmPassword, {
        message: "Passwords don't match",
        path: ['confirmPassword'],
    });

export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;

export const forgotPasswordSchema = z.object({
    email: z
        .string()
        .trim()
        .min(1, 'Email is required')
        .email('Please enter a valid email address')
        .max(AUTH_LIMITS.EMAIL_MAX, 'Email is too long'),
});

export const resetPasswordSchema = z
    .object({
        password: z
            .string()
            .min(
                AUTH_LIMITS.PASSWORD_MIN,
                `Password must be at least ${AUTH_LIMITS.PASSWORD_MIN} characters`,
            )
            .max(AUTH_LIMITS.PASSWORD_MAX, 'Password is too long')
            .regex(/[A-Z]/, 'Must contain at least one uppercase letter')
            .regex(/[a-z]/, 'Must contain at least one lowercase letter')
            .regex(/[0-9]/, 'Must contain at least one digit'),
        confirmPassword: z.string().min(1, 'Please confirm your password'),
    })
    .refine((data) => data.password === data.confirmPassword, {
        message: "Passwords don't match",
        path: ['confirmPassword'],
    });

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;
export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;
