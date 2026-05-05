import { z } from 'zod';

export const loginSchema = z.object({
    email: z
        .string()
        .min(1, 'Email is required')
        .email('Please enter a valid email address')
        .max(256, 'Email is too long'),
    password: z.string().min(1, 'Password is required').max(128, 'Password is too long'),
});

export const registerSchema = z
    .object({
        firstName: z.string().min(1, 'First name is required').max(100, 'First name is too long'),
        lastName: z.string().min(1, 'Last name is required').max(100, 'Last name is too long'),
        email: z
            .string()
            .min(1, 'Email is required')
            .email('Please enter a valid email address')
            .max(256, 'Email is too long'),
        password: z
            .string()
            .min(8, 'Password must be at least 8 characters')
            .max(128, 'Password is too long')
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
