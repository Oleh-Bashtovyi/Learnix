import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { GoogleLogin } from '@react-oauth/google';
import { Eye, EyeOff, CheckCircle2, Circle, Mail } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { authApi } from '@/api/auth.api';
import { registerSchema, type RegisterFormData } from '@/schemas/auth.schema';
import { useGoogleAuth } from '@/hooks/useGoogleAuth';
import { isValidationError, setApiFieldErrors } from '@/utils/errors';
import { cn } from '@/utils/cn';
import { Logo } from '@/components/common/Logo';

const REGISTER_FIELD_MAP: Partial<Record<string, keyof RegisterFormData>> = {
    Email: 'email',
    email: 'email',
    Password: 'password',
    password: 'password',
    FirstName: 'firstName',
    firstName: 'firstName',
    LastName: 'lastName',
    lastName: 'lastName',
};

function PasswordRule({ met, label }: { met: boolean; label: string }) {
    return (
        <li
            className={cn(
                'flex items-center gap-1.5 text-xs',
                met ? 'text-success' : 'text-muted-foreground',
            )}
        >
            {met ? (
                <CheckCircle2 className="h-3.5 w-3.5 shrink-0" />
            ) : (
                <Circle className="h-3.5 w-3.5 shrink-0" />
            )}
            {label}
        </li>
    );
}

export default function RegisterPage() {
    const { t } = useTranslation('auth');
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);
    const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);
    const [resendCooldown, setResendCooldown] = useState(0);

    useEffect(() => {
        if (resendCooldown <= 0) return;
        const timer = setTimeout(() => setResendCooldown((c) => c - 1), 1000);
        return () => clearTimeout(timer);
    }, [resendCooldown]);

    const {
        register,
        handleSubmit,
        watch,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<RegisterFormData>({
        resolver: zodResolver(registerSchema),
    });

    const { mutateAsync } = useMutation({
        mutationFn: authApi.register,
    });

    const { mutate: resendEmail, isPending: isResending } = useMutation({
        mutationFn: () => authApi.resendConfirmation({ email: registeredEmail! }),
        onSuccess: () => {
            setResendCooldown(60);
            toast.success('Confirmation email resent. Check your inbox.');
        },
        meta: { suppressGlobalError: true },
        onError: () => {
            toast.error('Failed to resend. Please try again later.');
        },
    });

    const { onGoogleCredential } = useGoogleAuth();

    const passwordValue = watch('password') ?? '';

    const passwordRules = {
        minLength: passwordValue.length >= 8,
        uppercase: /[A-Z]/.test(passwordValue),
        lowercase: /[a-z]/.test(passwordValue),
        digit: /[0-9]/.test(passwordValue),
    };

    const onSubmit = async (data: RegisterFormData) => {
        try {
            const { confirmPassword: _, ...payload } = data;
            await mutateAsync(payload);
            setRegisteredEmail(data.email);
        } catch (err) {
            if (isValidationError(err)) {
                setApiFieldErrors(err, setError, REGISTER_FIELD_MAP);
            } else {
                setError('root', {
                    type: 'server',
                    message:
                        err instanceof Error
                            ? err.message
                            : 'Registration failed. Please try again.',
                });
            }
        }
    };

    if (registeredEmail) {
        return (
            <div className="w-full max-w-[420px] py-12">
                <div className="rounded-2xl border border-border bg-card p-8 text-center shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                    <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
                        <Mail className="h-8 w-8 text-primary" />
                    </div>
                    <h1 className="font-heading text-2xl font-bold text-foreground">
                        {t('register.successTitle')}
                    </h1>
                    <p className="mt-3 text-sm text-muted-foreground">
                        {t('register.successMessage', { email: registeredEmail })}
                    </p>
                    <Link
                        to="/login"
                        className="mt-6 inline-flex w-full items-center justify-center rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90"
                    >
                        {t('register.backToLogin')}
                    </Link>
                    <button
                        type="button"
                        onClick={() => resendEmail()}
                        disabled={resendCooldown > 0 || isResending}
                        className="mt-3 w-full rounded-lg border border-border px-4 py-2.5 text-sm font-medium text-foreground transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {resendCooldown > 0
                            ? t('register.resendCooldown', { seconds: resendCooldown })
                            : isResending
                              ? '...'
                              : t('register.resend')}
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="w-full max-w-[480px] py-12">
            <div className="rounded-2xl border border-border bg-card p-8 shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                <div className="mb-8 text-center">
                    <Link
                        to="/"
                        className="mb-6 inline-flex items-center gap-2 font-heading font-bold"
                    >
                        <div className="grid h-9 w-9 place-items-center rounded-lg bg-primary font-heading text-lg font-bold text-primary-foreground">
                            <Logo className="h-6 w-6" />
                        </div>
                        <span className="text-xl">Learnix</span>
                    </Link>
                    <h1 className="font-heading text-2xl font-bold text-foreground">
                        {t('register.title')}
                    </h1>
                    <p className="mt-1 text-sm text-muted-foreground">{t('register.subtitle')}</p>
                </div>

                <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
                    {errors.root && (
                        <div className="rounded-lg border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
                            {errors.root.message}
                        </div>
                    )}

                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label
                                htmlFor="firstName"
                                className="mb-1.5 block text-sm font-medium text-foreground"
                            >
                                {t('register.firstName.label')}
                            </label>
                            <input
                                id="firstName"
                                type="text"
                                autoComplete="given-name"
                                placeholder={t('register.firstName.placeholder')}
                                {...register('firstName')}
                                className={cn(
                                    'w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
                                    'focus:border-primary focus:ring-2 focus:ring-primary/10',
                                    errors.firstName
                                        ? 'border-destructive focus:border-destructive focus:ring-destructive/10'
                                        : 'border-border',
                                )}
                            />
                            {errors.firstName && (
                                <p className="mt-1.5 text-xs text-destructive">
                                    {errors.firstName.message}
                                </p>
                            )}
                        </div>

                        <div>
                            <label
                                htmlFor="lastName"
                                className="mb-1.5 block text-sm font-medium text-foreground"
                            >
                                {t('register.lastName.label')}
                            </label>
                            <input
                                id="lastName"
                                type="text"
                                autoComplete="family-name"
                                placeholder={t('register.lastName.placeholder')}
                                {...register('lastName')}
                                className={cn(
                                    'w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
                                    'focus:border-primary focus:ring-2 focus:ring-primary/10',
                                    errors.lastName
                                        ? 'border-destructive focus:border-destructive focus:ring-destructive/10'
                                        : 'border-border',
                                )}
                            />
                            {errors.lastName && (
                                <p className="mt-1.5 text-xs text-destructive">
                                    {errors.lastName.message}
                                </p>
                            )}
                        </div>
                    </div>

                    <div>
                        <label
                            htmlFor="email"
                            className="mb-1.5 block text-sm font-medium text-foreground"
                        >
                            {t('register.email.label')}
                        </label>
                        <input
                            id="email"
                            type="email"
                            autoComplete="email"
                            placeholder={t('register.email.placeholder')}
                            {...register('email')}
                            className={cn(
                                'w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
                                'focus:border-primary focus:ring-2 focus:ring-primary/10',
                                errors.email
                                    ? 'border-destructive focus:border-destructive focus:ring-destructive/10'
                                    : 'border-border',
                            )}
                        />
                        {errors.email && (
                            <p className="mt-1.5 text-xs text-destructive">
                                {errors.email.message}
                            </p>
                        )}
                    </div>

                    <div>
                        <label
                            htmlFor="password"
                            className="mb-1.5 block text-sm font-medium text-foreground"
                        >
                            {t('register.password.label')}
                        </label>
                        <div className="relative">
                            <input
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                autoComplete="new-password"
                                placeholder={t('register.password.placeholder')}
                                {...register('password')}
                                className={cn(
                                    'w-full rounded-lg border bg-background py-2.5 pl-3.5 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
                                    'focus:border-primary focus:ring-2 focus:ring-primary/10',
                                    errors.password
                                        ? 'border-destructive focus:border-destructive focus:ring-destructive/10'
                                        : 'border-border',
                                )}
                            />
                            <button
                                type="button"
                                onClick={() => setShowPassword((v) => !v)}
                                className="absolute inset-y-0 right-0 flex items-center px-3 text-muted-foreground hover:text-foreground"
                                tabIndex={-1}
                            >
                                {showPassword ? (
                                    <EyeOff className="h-4 w-4" />
                                ) : (
                                    <Eye className="h-4 w-4" />
                                )}
                            </button>
                        </div>
                        {passwordValue && (
                            <ul className="mt-2 space-y-1 pl-0.5">
                                <PasswordRule
                                    met={passwordRules.minLength}
                                    label={t('register.password.requirements.minLength')}
                                />
                                <PasswordRule
                                    met={passwordRules.uppercase}
                                    label={t('register.password.requirements.uppercase')}
                                />
                                <PasswordRule
                                    met={passwordRules.lowercase}
                                    label={t('register.password.requirements.lowercase')}
                                />
                                <PasswordRule
                                    met={passwordRules.digit}
                                    label={t('register.password.requirements.digit')}
                                />
                            </ul>
                        )}
                        {errors.password && (
                            <p className="mt-1.5 text-xs text-destructive">
                                {errors.password.message}
                            </p>
                        )}
                    </div>

                    <div>
                        <label
                            htmlFor="confirmPassword"
                            className="mb-1.5 block text-sm font-medium text-foreground"
                        >
                            {t('register.confirmPassword.label')}
                        </label>
                        <div className="relative">
                            <input
                                id="confirmPassword"
                                type={showConfirm ? 'text' : 'password'}
                                autoComplete="new-password"
                                placeholder={t('register.confirmPassword.placeholder')}
                                {...register('confirmPassword')}
                                className={cn(
                                    'w-full rounded-lg border bg-background py-2.5 pl-3.5 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground',
                                    'focus:border-primary focus:ring-2 focus:ring-primary/10',
                                    errors.confirmPassword
                                        ? 'border-destructive focus:border-destructive focus:ring-destructive/10'
                                        : 'border-border',
                                )}
                            />
                            <button
                                type="button"
                                onClick={() => setShowConfirm((v) => !v)}
                                className="absolute inset-y-0 right-0 flex items-center px-3 text-muted-foreground hover:text-foreground"
                                tabIndex={-1}
                            >
                                {showConfirm ? (
                                    <EyeOff className="h-4 w-4" />
                                ) : (
                                    <Eye className="h-4 w-4" />
                                )}
                            </button>
                        </div>
                        {errors.confirmPassword && (
                            <p className="mt-1.5 text-xs text-destructive">
                                {errors.confirmPassword.message}
                            </p>
                        )}
                    </div>

                    <button
                        type="submit"
                        disabled={isSubmitting}
                        className="mt-2 w-full rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {isSubmitting ? t('register.submitting') : t('register.submit')}
                    </button>
                </form>

                <div className="my-6 flex items-center gap-3">
                    <div className="h-px flex-1 bg-border" />
                    <span className="text-xs text-muted-foreground">{t('register.divider')}</span>
                    <div className="h-px flex-1 bg-border" />
                </div>

                <div className="flex justify-center">
                    <GoogleLogin
                        onSuccess={(response) => {
                            if (response.credential) {
                                onGoogleCredential(response.credential);
                            }
                        }}
                        onError={() => toast.error(t('register.googleError'))}
                        theme="outline"
                        size="large"
                        shape="rectangular"
                        text="signup_with"
                        width={416}
                    />
                </div>

                <p className="mt-6 text-center text-sm text-muted-foreground">
                    {t('register.hasAccount')}{' '}
                    <Link to="/login" className="font-medium text-primary hover:underline">
                        {t('register.login')}
                    </Link>
                </p>
            </div>
        </div>
    );
}
