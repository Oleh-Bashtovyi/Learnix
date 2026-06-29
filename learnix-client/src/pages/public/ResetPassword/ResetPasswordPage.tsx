import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { Eye, EyeOff } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { Logo } from '@/components/common/ui/Logo';
import { APP_ROUTES } from '@/routes/paths';
import { type ResetPasswordFormData, resetPasswordSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';
import { getErrorMessage, isValidationError, setApiFieldErrors } from '@/utils/errors';

const RESET_FIELD_MAP: Partial<Record<string, keyof ResetPasswordFormData>> = {
    NewPassword: 'password',
    newPassword: 'password',
    ConfirmPassword: 'confirmPassword',
    confirmPassword: 'confirmPassword',
};

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-003: Token-Based Password Reset Flow
 */
export default function ResetPasswordPage() {
    const { t } = useTranslation('auth');
    const navigate = useNavigate();
    const { user } = useAuthStore();
    const isAuthenticated = !!user;
    const [searchParams] = useSearchParams();
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    const email = searchParams.get('email');
    const token = searchParams.get('token');

    const {
        register,
        handleSubmit,
        setError,
        clearErrors,
        formState: { errors, isSubmitting },
    } = useForm<ResetPasswordFormData>({
        resolver: zodResolver(resetPasswordSchema),
    });

    const { mutateAsync } = useMutation({
        mutationFn: authApi.resetPassword,
        meta: { suppressGlobalError: true },
    });

    const onSubmit = async (data: ResetPasswordFormData) => {
        if (!email || !token) {
            setError('root', { type: 'custom', message: t('resetPassword.invalidToken') });
            return;
        }

        try {
            await mutateAsync({
                email,
                token,
                newPassword: data.password,
            });
            toast.success(t('resetPassword.successMessage'));
            navigate(APP_ROUTES.public.login, { replace: true });
        } catch (err) {
            if (isValidationError(err)) {
                setApiFieldErrors(err, setError, RESET_FIELD_MAP);
            } else {
                setError('root', {
                    type: 'server',
                    message: getErrorMessage(err, 'Failed to reset password.'),
                });
            }
        }
    };

    if (!email || !token) {
        return (
            <div className="w-full max-w-[420px] py-12">
                <div className="rounded-2xl border border-border bg-card p-8 text-center shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                    <h1 className="font-heading text-xl font-bold text-foreground">
                        {t('resetPassword.invalidToken')}
                    </h1>
                    <div className="mt-6">
                        {isAuthenticated ? (
                            <Link
                                to={APP_ROUTES.student.profile}
                                className="text-sm font-medium text-primary hover:underline"
                            >
                                {t('backToProfile')}
                            </Link>
                        ) : (
                            <Link
                                to={APP_ROUTES.public.login}
                                className="text-sm font-medium text-primary hover:underline"
                            >
                                {t('resetPassword.backToLogin')}
                            </Link>
                        )}
                    </div>
                </div>
            </div>
        );
    }

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
                        {t('resetPassword.title')}
                    </h1>
                    <p className="mt-2 text-sm text-muted-foreground">
                        {t('resetPassword.subtitle')}
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
                            htmlFor="password"
                            className="mb-1.5 block text-sm font-medium text-foreground"
                        >
                            {t('resetPassword.password.label')}
                        </label>
                        <div className="relative">
                            <input
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                autoComplete="new-password"
                                placeholder={t('resetPassword.password.placeholder')}
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

                    <div>
                        <label
                            htmlFor="confirmPassword"
                            className="mb-1.5 block text-sm font-medium text-foreground"
                        >
                            {t('resetPassword.confirmPassword.label')}
                        </label>
                        <div className="relative">
                            <input
                                id="confirmPassword"
                                type={showConfirmPassword ? 'text' : 'password'}
                                autoComplete="new-password"
                                placeholder={t('resetPassword.confirmPassword.placeholder')}
                                {...register('confirmPassword', {
                                    onChange: () => clearErrors('root'),
                                })}
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
                                onClick={() => setShowConfirmPassword((v) => !v)}
                                className="absolute inset-y-0 right-0 flex items-center px-3 text-muted-foreground hover:text-foreground"
                                tabIndex={-1}
                            >
                                {showConfirmPassword ? (
                                    <EyeOff className="size-4" />
                                ) : (
                                    <Eye className="size-4" />
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
                        className="mt-4 w-full rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {isSubmitting ? t('resetPassword.submitting') : t('resetPassword.submit')}
                    </button>
                </form>

                <div className="mt-6 text-center">
                    {isAuthenticated ? (
                        <Link
                            to={APP_ROUTES.student.profile}
                            className="text-sm font-medium text-primary hover:underline"
                        >
                            {t('backToProfile')}
                        </Link>
                    ) : (
                        <Link
                            to={APP_ROUTES.public.login}
                            className="text-sm font-medium text-primary hover:underline"
                        >
                            {t('resetPassword.backToLogin')}
                        </Link>
                    )}
                </div>
            </div>
        </div>
    );
}
