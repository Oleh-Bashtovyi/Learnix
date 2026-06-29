import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { MailCheck } from 'lucide-react';
import { authApi } from '@/api/auth.api';
import { Logo } from '@/components/common/ui/Logo';
import { APP_ROUTES } from '@/routes/paths';
import { type ForgotPasswordFormData, forgotPasswordSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';
import { getErrorMessage, isValidationError, setApiFieldErrors } from '@/utils/errors';

const FORGOT_FIELD_MAP: Partial<Record<string, keyof ForgotPasswordFormData>> = {
    Email: 'email',
    email: 'email',
};

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-003: Token-Based Password Reset Flow
 */
export default function ForgotPasswordPage() {
    const { t } = useTranslation('auth');
    const { user } = useAuthStore();
    const isAuthenticated = !!user;
    const [isSuccess, setIsSuccess] = useState(false);
    const [submittedEmail, setSubmittedEmail] = useState('');

    const {
        register,
        handleSubmit,
        setError,
        clearErrors,
        formState: { errors, isSubmitting },
    } = useForm<ForgotPasswordFormData>({
        resolver: zodResolver(forgotPasswordSchema),
        defaultValues: {
            email: isAuthenticated && user ? user.email : '',
        },
    });

    const { mutateAsync } = useMutation({
        mutationFn: authApi.forgotPassword,
        meta: { suppressGlobalError: true },
    });

    const onSubmit = async (data: ForgotPasswordFormData) => {
        try {
            await mutateAsync(data);
            setSubmittedEmail(data.email);
            setIsSuccess(true);
        } catch (err) {
            if (isValidationError(err)) {
                setApiFieldErrors(err, setError, FORGOT_FIELD_MAP);
            } else {
                setError('root', {
                    type: 'server',
                    message: getErrorMessage(err, 'Failed to send reset instructions.'),
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

                    {!isSuccess ? (
                        <>
                            <h1 className="font-heading text-2xl font-bold text-foreground">
                                {t('forgotPassword.title')}
                            </h1>
                            <p className="mt-2 text-sm text-muted-foreground">
                                {t('forgotPassword.subtitle')}
                            </p>
                        </>
                    ) : (
                        <>
                            <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full bg-primary/10">
                                <MailCheck className="size-6 text-primary" />
                            </div>
                            <h1 className="font-heading text-2xl font-bold text-foreground">
                                {t('forgotPassword.successTitle')}
                            </h1>
                            <p className="mt-2 text-sm text-muted-foreground">
                                {t('forgotPassword.successMessage', { email: submittedEmail })}
                            </p>
                        </>
                    )}
                </div>

                {!isSuccess ? (
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
                                {t('forgotPassword.email.label')}
                            </label>
                            <input
                                id="email"
                                type="email"
                                autoComplete="email"
                                placeholder={t('forgotPassword.email.placeholder')}
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

                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="mt-2 w-full rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                            {isSubmitting
                                ? t('forgotPassword.submitting')
                                : t('forgotPassword.submit')}
                        </button>
                    </form>
                ) : null}

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
                            {t('forgotPassword.backToLogin')}
                        </Link>
                    )}
                </div>
            </div>
        </div>
    );
}
