import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { GoogleLogin } from '@react-oauth/google';
import { useMutation } from '@tanstack/react-query';
import { CheckCircle2, Circle } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { FormInput } from '@/components/common/form/FormInput';
import { PasswordInput } from '@/components/common/form/PasswordInput';
import { Logo } from '@/components/common/ui/Logo';
import { useGoogleAuth } from '@/hooks/auth/useGoogleAuth';
import { APP_ROUTES } from '@/routes/paths';
import { type RegisterFormData, registerSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import type { LocationStateWithFrom } from '@/types/router.types';
import { cn } from '@/utils/cn';
import { isValidationError, setApiFieldErrors } from '@/utils/errors';
import { parseAccessToken } from '@/utils/parseAccessToken';

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

type PasswordRuleProps = {
    met: boolean;
    label: string;
};

function PasswordRule({ met, label }: PasswordRuleProps) {
    return (
        <li
            className={cn(
                'flex items-center gap-1.5 text-xs',
                met ? 'text-success' : 'text-muted-foreground',
            )}
        >
            {met ? (
                <CheckCircle2 className="size-3.5 shrink-0" />
            ) : (
                <Circle className="size-3.5 shrink-0" />
            )}
            {label}
        </li>
    );
}

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-002: OTP-Based Email Verification & Auto-Login
 */
export default function RegisterPage() {
    const { t } = useTranslation('auth');
    const navigate = useNavigate();
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);

    const {
        register,
        handleSubmit,
        control,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<RegisterFormData>({
        resolver: zodResolver(registerSchema),
    });

    const { mutateAsync } = useMutation({
        mutationFn: authApi.register,
    });

    const { onGoogleCredential } = useGoogleAuth();

    const passwordValue = useWatch({ control, name: 'password' }) ?? '';

    const passwordRules = {
        minLength: passwordValue.length >= 8,
        uppercase: /[A-Z]/.test(passwordValue),
        lowercase: /[a-z]/.test(passwordValue),
        digit: /[0-9]/.test(passwordValue),
    };

    const onSubmit = async (data: RegisterFormData) => {
        try {
            const { confirmPassword: _, ...payload } = data;
            const response = await mutateAsync(payload);
            navigate(APP_ROUTES.public.verifyEmail, { state: { email: data.email } });

            setTimeout(() => {
                setAccessToken(response.accessToken);
                const user = parseAccessToken(response.accessToken);
                if (user) setUser({ ...user, avatarUrl: response.avatarUrl });
            }, 0);
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

    return (
        <div className="w-full max-w-[480px] py-12">
            <div className="rounded-2xl border border-border bg-card p-5 shadow-[0_4px_20px_rgba(59,130,246,0.05)] sm:p-8">
                <div className="mb-8 text-center">
                    <Link
                        to={APP_ROUTES.public.home}
                        className="mb-6 inline-flex items-center gap-2 font-heading font-bold"
                    >
                        <div className="grid size-9 place-items-center rounded-lg bg-primary font-heading text-lg font-bold text-primary-foreground">
                            <Logo className="size-6" />
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
                        <FormInput
                            id="firstName"
                            autoComplete="given-name"
                            label={t('register.firstName.label')}
                            placeholder={t('register.firstName.placeholder')}
                            error={errors.firstName?.message}
                            className="border-border px-3.5 py-2.5 focus:border-primary focus:ring-primary/10"
                            {...register('firstName')}
                        />

                        <FormInput
                            id="lastName"
                            autoComplete="family-name"
                            label={t('register.lastName.label')}
                            placeholder={t('register.lastName.placeholder')}
                            error={errors.lastName?.message}
                            className="border-border px-3.5 py-2.5 focus:border-primary focus:ring-primary/10"
                            {...register('lastName')}
                        />
                    </div>

                    <FormInput
                        id="email"
                        type="email"
                        autoComplete="email"
                        label={t('register.email.label')}
                        placeholder={t('register.email.placeholder')}
                        error={errors.email?.message}
                        className="border-border px-3.5 py-2.5 focus:border-primary focus:ring-primary/10"
                        {...register('email')}
                    />

                    <div>
                        <PasswordInput
                            id="password"
                            autoComplete="new-password"
                            label={t('register.password.label')}
                            placeholder={t('register.password.placeholder')}
                            error={errors.password?.message}
                            className="border-border py-2.5 pl-3.5 focus:border-primary focus:ring-primary/10"
                            {...register('password')}
                        />
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
                    </div>

                    <PasswordInput
                        id="confirmPassword"
                        autoComplete="new-password"
                        label={t('register.confirmPassword.label')}
                        placeholder={t('register.confirmPassword.placeholder')}
                        error={errors.confirmPassword?.message}
                        className="border-border py-2.5 pl-3.5 focus:border-primary focus:ring-primary/10"
                        {...register('confirmPassword')}
                    />

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

                <div className="mx-auto flex w-fit justify-center">
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
                    />
                </div>

                <p className="mt-6 text-center text-sm text-muted-foreground">
                    {t('register.hasAccount')}{' '}
                    <Link
                        to={APP_ROUTES.public.login}
                        state={{ from: (location.state as LocationStateWithFrom | null)?.from }}
                        className="font-medium text-primary hover:underline"
                    >
                        {t('register.login')}
                    </Link>
                </p>
            </div>
        </div>
    );
}
