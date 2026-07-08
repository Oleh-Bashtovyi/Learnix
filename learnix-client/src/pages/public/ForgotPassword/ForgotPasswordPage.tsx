import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { MailCheck } from 'lucide-react';
import { type ForgotPasswordRequest, authApi } from '@/api/auth.api';
import { AuthCard } from '@/components/common/auth/AuthCard';
import { AuthFooter } from '@/components/common/auth/AuthFooter';
import { AuthHeader } from '@/components/common/auth/AuthHeader';
import { FormErrorAlert } from '@/components/common/form/FormErrorAlert';
import { FormInput } from '@/components/common/form/FormInput';
import { AsyncButton } from '@/components/ui/async-button';
import { AUTH_LIMITS } from '@/const/auth.constants';
import { APP_ROUTES } from '@/routes/paths';
import { type ForgotPasswordFormData, forgotPasswordSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import { handleFormError } from '@/utils/errors';
import { type ApiFieldMap } from '@/utils/errors';

const FORGOT_FIELD_MAP: ApiFieldMap<ForgotPasswordRequest, ForgotPasswordFormData> = {
    email: 'email',
};

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-003: Token-Based Password Reset Flow
 * - ADR-FRONT-FORMS-005: Centralized Form Error Handling
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
        formState: { errors, isSubmitting, isSubmitSuccessful },
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
            handleFormError(err, setError, t('forgotPassword.errors.fallback'), FORGOT_FIELD_MAP);
        }
    };

    return (
        <AuthCard>
            <AuthHeader
                title={t('forgotPassword.title')}
                subtitle={!isSuccess ? t('forgotPassword.subtitle') : ''}
            />

            {isSuccess && (
                <div className="mb-8 text-center">
                    <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full bg-primary/10">
                        <MailCheck className="size-6 text-primary" />
                    </div>
                    <h2 className="font-heading text-2xl font-bold text-foreground">
                        {t('forgotPassword.successTitle')}
                    </h2>
                    <p className="mt-2 text-sm text-muted-foreground">
                        {t('forgotPassword.successMessage', { email: submittedEmail })}
                    </p>
                </div>
            )}

            {!isSuccess ? (
                <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
                    <FormErrorAlert message={errors.root?.message} />

                    <FormInput
                        id="email"
                        type="email"
                        autoComplete="email"
                        variant="auth"
                        label={t('forgotPassword.email.label')}
                        placeholder={t('forgotPassword.email.placeholder')}
                        error={errors.email}
                        maxLength={AUTH_LIMITS.EMAIL_MAX}
                        {...register('email', { onChange: () => clearErrors('root') })}
                    />

                    <AsyncButton
                        type="submit"
                        disabled={isSubmitting}
                        isLoading={isSubmitting}
                        isSuccess={isSubmitSuccessful}
                        loadingText={t('common:actions.sending')}
                        className="mt-2 w-full"
                    >
                        {t('forgotPassword.submit')}
                    </AsyncButton>
                </form>
            ) : null}

            <AuthFooter
                linkText={isAuthenticated ? t('backToProfile') : t('forgotPassword.backToLogin')}
                linkTo={isAuthenticated ? APP_ROUTES.student.profile : APP_ROUTES.public.login}
            />
        </AuthCard>
    );
}
