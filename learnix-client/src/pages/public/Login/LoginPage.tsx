import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { GoogleLogin } from '@react-oauth/google';
import { useMutation } from '@tanstack/react-query';
import { Eye, EyeOff } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { Logo } from '@/components/common/ui/Logo';
import { useGoogleAuth } from '@/hooks/auth/useGoogleAuth';
import { APP_ROUTES } from '@/routes/paths';
import { type LoginFormData, loginSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';
import { getErrorMessage, isValidationError, setApiFieldErrors } from '@/utils/errors';
import { getRoleHome } from '@/utils/getRoleHome';
import { parseAccessToken } from '@/utils/parseAccessToken';

const LOGIN_FIELD_MAP: Partial<Record<string, keyof LoginFormData>> = {
    Email: 'email',
    email: 'email',
    Password: 'password',
    password: 'password',
};

export default function LoginPage() {
    const { t } = useTranslation('auth');
    const navigate = useNavigate();
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);
    const [showPassword, setShowPassword] = useState(false);

    const from = (location.state as { from?: Location })?.from?.pathname;

    const {
        register,
        handleSubmit,
        setError,
        resetField,
        clearErrors,
        formState: { errors, isSubmitting },
    } = useForm<LoginFormData>({
        resolver: zodResolver(loginSchema),
    });

    /**
     * Related ADRs:
     * - ADR-FRONT-FORMS-004: Form Errors vs Global Errors
     */
    const { mutateAsync } = useMutation({
        mutationFn: authApi.login,
        meta: { suppressGlobalError: true },
    });

    const { onGoogleCredential } = useGoogleAuth();

    const onSubmit = async (data: LoginFormData) => {
        try {
            // ADR-FRONT-FORMS-002: Explicit transformation from FormValues to DTO
            const response = await mutateAsync(data as Parameters<typeof authApi.login>[0]);
            setAccessToken(response.accessToken);
            const user = parseAccessToken(response.accessToken);
            if (user) setUser({ ...user, avatarUrl: response.avatarUrl });
            toast.success(t('login.successRedirect'));
            navigate(from ?? (user ? getRoleHome(user.roles) : APP_ROUTES.public.courses), {
                replace: true,
            });
        } catch (err) {
            resetField('password');
            if (isValidationError(err)) {
                setApiFieldErrors(err, setError, LOGIN_FIELD_MAP);
            } else {
                setError('root', {
                    type: 'server',
                    message: getErrorMessage(err, 'Login failed. Please try again.'),
                });
            }
        }
    };

    return (
        <div className="w-full max-w-[420px] py-12">
            <div className="rounded-2xl border border-border bg-card p-8 shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
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
                        {t('login.title')}
                    </h1>
                    <p className="mt-1 text-sm text-muted-foreground">
                        {t('login.subtitle') || ''}
                    </p>
                </div>

                <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
                    {errors.root && (
                        <div className="rounded-lg border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
                            {errors.root.message}
                        </div>
                    )}

                    <div>
                        <label
                            htmlFor="email"
                            className="mb-1.5 block text-sm font-medium text-foreground"
                        >
                            {t('login.email.label')}
                        </label>
                        <input
                            id="email"
                            type="email"
                            autoComplete="email"
                            placeholder={t('login.email.placeholder')}
                            {...register('email', { onChange: () => clearErrors('root') })}
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
                        <div className="mb-1.5 flex items-center justify-between">
                            <label
                                htmlFor="password"
                                className="text-sm font-medium text-foreground"
                            >
                                {t('login.password.label')}
                            </label>
                            <Link
                                to={APP_ROUTES.public.forgotPassword}
                                className="text-xs text-primary hover:underline"
                            >
                                {t('login.forgotPassword')}
                            </Link>
                        </div>
                        <div className="relative">
                            <input
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                autoComplete="current-password"
                                placeholder={t('login.password.placeholder')}
                                {...register('password', { onChange: () => clearErrors('root') })}
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
                                    <EyeOff className="size-4" />
                                ) : (
                                    <Eye className="size-4" />
                                )}
                            </button>
                        </div>
                        {errors.password && (
                            <p className="mt-1.5 text-xs text-destructive">
                                {errors.password.message}
                            </p>
                        )}
                    </div>

                    <button
                        type="submit"
                        disabled={isSubmitting}
                        className="mt-2 w-full rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {isSubmitting ? t('login.submitting') : t('login.submit')}
                    </button>
                </form>

                <div className="my-6 flex items-center gap-3">
                    <div className="h-px flex-1 bg-border" />
                    <span className="text-xs text-muted-foreground">{t('login.divider')}</span>
                    <div className="h-px flex-1 bg-border" />
                </div>

                <div className="flex justify-center">
                    <GoogleLogin
                        onSuccess={(response) => {
                            if (response.credential) {
                                onGoogleCredential(response.credential);
                            }
                        }}
                        onError={() => toast.error(t('login.googleError'))}
                        theme="outline"
                        size="large"
                        shape="rectangular"
                        text="continue_with"
                        width={356}
                    />
                </div>

                <p className="mt-6 text-center text-sm text-muted-foreground">
                    {t('login.noAccount')}{' '}
                    <Link
                        to={APP_ROUTES.public.register}
                        className="font-medium text-primary hover:underline"
                    >
                        {t('login.register')}
                    </Link>
                </p>
            </div>
        </div>
    );
}
