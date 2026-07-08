import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { type ResetPasswordRequest, authApi } from '@/api/auth.api';
import { AuthCard } from '@/components/common/auth/AuthCard';
import { AuthFooter } from '@/components/common/auth/AuthFooter';
import { AuthHeader } from '@/components/common/auth/AuthHeader';
import { FormErrorAlert } from '@/components/common/form/FormErrorAlert';
import { PasswordInput } from '@/components/common/form/PasswordInput';
import { AsyncButton } from '@/components/ui/async-button';
import { AUTH_LIMITS } from '@/const/auth.constants';
import { APP_ROUTES } from '@/routes/paths';
import { type ResetPasswordFormData, resetPasswordSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import { type ApiFieldMap, handleFormError } from '@/utils/errors';

const RESET_FIELD_MAP: ApiFieldMap<ResetPasswordRequest, ResetPasswordFormData> = {
    newPassword: 'password',
};

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-003: Token-Based Password Reset Flow
 * - ADR-FRONT-FORMS-005: Centralized Form Error Handling
 */
export default function ResetPasswordPage() {
    const { t } = useTranslation('auth');
    const navigate = useNavigate();
    const { user } = useAuthStore();
    const isAuthenticated = !!user;
    const [searchParams] = useSearchParams();

    const email = searchParams.get('email');
    const token = searchParams.get('token');

    const {
        register,
        handleSubmit,
        setError,
        clearErrors,
        formState: { errors, isSubmitting, isSubmitSuccessful },
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
            handleFormError(err, setError, t('resetPassword.errors.fallback'), RESET_FIELD_MAP);
        }
    };

    if (!email || !token) {
        return (
            <AuthCard>
                <div className="text-center">
                    <h1 className="font-heading text-xl font-bold text-foreground">
                        {t('resetPassword.invalidToken')}
                    </h1>
                    <AuthFooter
                        linkText={
                            isAuthenticated ? t('backToProfile') : t('resetPassword.backToLogin')
                        }
                        linkTo={
                            isAuthenticated ? APP_ROUTES.student.profile : APP_ROUTES.public.login
                        }
                    />
                </div>
            </AuthCard>
        );
    }

    return (
        <AuthCard>
            <AuthHeader title={t('resetPassword.title')} subtitle={t('resetPassword.subtitle')} />

            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
                <FormErrorAlert message={errors.root?.message} />

                <PasswordInput
                    id="password"
                    autoComplete="new-password"
                    variant="auth"
                    label={t('resetPassword.password.label')}
                    placeholder={t('resetPassword.password.placeholder')}
                    error={errors.password}
                    maxLength={AUTH_LIMITS.PASSWORD_MAX}
                    {...register('password', { onChange: () => clearErrors('root') })}
                />

                <PasswordInput
                    id="confirmPassword"
                    autoComplete="new-password"
                    variant="auth"
                    label={t('resetPassword.confirmPassword.label')}
                    placeholder={t('resetPassword.confirmPassword.placeholder')}
                    error={errors.confirmPassword}
                    maxLength={AUTH_LIMITS.PASSWORD_MAX}
                    {...register('confirmPassword', { onChange: () => clearErrors('root') })}
                />

                <AsyncButton
                    type="submit"
                    disabled={isSubmitting}
                    isLoading={isSubmitting}
                    isSuccess={isSubmitSuccessful}
                    loadingText={t('resetPassword.submitting')}
                    className="mt-4 w-full"
                >
                    {t('resetPassword.submit')}
                </AsyncButton>
            </form>

            <AuthFooter
                linkText={isAuthenticated ? t('backToProfile') : t('resetPassword.backToLogin')}
                linkTo={isAuthenticated ? APP_ROUTES.student.profile : APP_ROUTES.public.login}
            />
        </AuthCard>
    );
}
