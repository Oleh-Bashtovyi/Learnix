import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { type LoginRequest, authApi } from '@/api/auth.api';
import { AuthCard } from '@/components/common/auth/AuthCard';
import { AuthDivider } from '@/components/common/auth/AuthDivider';
import { AuthFooter } from '@/components/common/auth/AuthFooter';
import { AuthHeader } from '@/components/common/auth/AuthHeader';
import { GoogleAuthButton } from '@/components/common/auth/GoogleAuthButton';
import { FormErrorAlert } from '@/components/common/form/FormErrorAlert';
import { FormInput } from '@/components/common/form/FormInput';
import { PasswordInput } from '@/components/common/form/PasswordInput';
import { TextLink } from '@/components/common/ui/TextLink';
import { AsyncButton } from '@/components/ui/async-button';
import { AUTH_LIMITS } from '@/const/auth.constants';
import { useGoogleAuth } from '@/hooks/auth/useGoogleAuth';
import { APP_ROUTES } from '@/routes/paths';
import { type LoginFormData, loginSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import type { LocationStateWithFrom } from '@/types/router.types';
import { type ApiFieldMap, handleFormError } from '@/utils/errors';
import { getRoleHome } from '@/utils/getRoleHome';
import { parseAccessToken } from '@/utils/parseAccessToken';

const LOGIN_FIELD_MAP: ApiFieldMap<LoginRequest, LoginFormData> = {
    email: 'email',
    password: 'password',
};

export default function LoginPage() {
    const { t } = useTranslation('auth');
    const navigate = useNavigate();
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);

    const from = (location.state as LocationStateWithFrom | null)?.from?.pathname;

    const {
        register,
        handleSubmit,
        setError,
        resetField,
        clearErrors,
        formState: { errors, isSubmitting, isSubmitSuccessful },
    } = useForm<LoginFormData>({
        resolver: zodResolver(loginSchema),
    });

    /**
     * Related ADRs:
     * - ADR-FRONT-FORMS-005: Centralized Form Error Handling
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
            handleFormError(err, setError, t('login.errors.fallback'), LOGIN_FIELD_MAP);
        }
    };

    return (
        <AuthCard>
            <AuthHeader title={t('login.title')} subtitle={t('login.subtitle')} />

            <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-1">
                <FormErrorAlert message={errors.root?.message} />

                <FormInput
                    id="email"
                    type="email"
                    autoComplete="email"
                    variant="auth"
                    label={t('login.email.label')}
                    placeholder={t('login.email.placeholder')}
                    error={errors.email}
                    maxLength={AUTH_LIMITS.EMAIL_MAX}
                    {...register('email', { onChange: () => clearErrors('root') })}
                />

                <PasswordInput
                    id="password"
                    autoComplete="current-password"
                    variant="auth"
                    label={t('login.password.label')}
                    labelRightAction={
                        <TextLink to={APP_ROUTES.public.forgotPassword} className="text-sm">
                            {t('login.forgotPassword')}
                        </TextLink>
                    }
                    placeholder={t('login.password.placeholder')}
                    error={errors.password}
                    maxLength={AUTH_LIMITS.PASSWORD_MAX}
                    {...register('password', { onChange: () => clearErrors('root') })}
                />

                <AsyncButton
                    type="submit"
                    disabled={isSubmitting}
                    isLoading={isSubmitting}
                    isSuccess={isSubmitSuccessful}
                    loadingText={t('login.submitting')}
                    className="mt-4 w-full bg-primary transition-all hover:scale-[1.01] hover:bg-primary/90 hover:shadow-lg hover:shadow-primary/20"
                >
                    {t('common:actions.logIn')}
                </AsyncButton>
            </form>

            <AuthDivider text={t('login.divider')} />

            <GoogleAuthButton
                text={t('login.google')}
                onSuccess={onGoogleCredential}
                onError={() => toast.error(t('login.googleError'))}
            />

            <AuthFooter
                text={t('login.noAccount')}
                linkText={t('login.register')}
                linkTo={APP_ROUTES.public.register}
                linkState={{ from: (location.state as LocationStateWithFrom | null)?.from }}
            />
        </AuthCard>
    );
}
