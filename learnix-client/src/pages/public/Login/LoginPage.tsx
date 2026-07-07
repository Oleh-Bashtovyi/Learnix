import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { GoogleLogin } from '@react-oauth/google';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { type LoginRequest, authApi } from '@/api/auth.api';
import { AuthCard } from '@/components/common/auth/AuthCard';
import { AuthFooter } from '@/components/common/auth/AuthFooter';
import { AuthHeader } from '@/components/common/auth/AuthHeader';
import { FormErrorAlert } from '@/components/common/form/FormErrorAlert';
import { FormInput } from '@/components/common/form/FormInput';
import { PasswordInput } from '@/components/common/form/PasswordInput';
import { Button } from '@/components/ui/button';
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
        formState: { errors, isSubmitting },
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

            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
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

                <div>
                    <div className="mb-1.5 flex items-center justify-end">
                        <Link
                            to={APP_ROUTES.public.forgotPassword}
                            className="text-xs text-primary hover:underline"
                        >
                            {t('login.forgotPassword')}
                        </Link>
                    </div>
                    <PasswordInput
                        id="password"
                        autoComplete="current-password"
                        variant="auth"
                        label={t('login.password.label')}
                        placeholder={t('login.password.placeholder')}
                        error={errors.password}
                        maxLength={AUTH_LIMITS.PASSWORD_MAX}
                        {...register('password', { onChange: () => clearErrors('root') })}
                    />
                </div>

                <Button type="submit" disabled={isSubmitting} className="mt-2 w-full">
                    {isSubmitting ? t('login.submitting') : t('common:actions.logIn')}
                </Button>
            </form>

            <div className="my-6 flex items-center gap-3">
                <div className="h-px flex-1 bg-border" />
                <span className="text-xs text-muted-foreground">{t('login.divider')}</span>
                <div className="h-px flex-1 bg-border" />
            </div>

            <div className="mx-auto flex w-fit justify-center">
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
                />
            </div>

            <AuthFooter
                text={t('login.noAccount')}
                linkText={t('login.register')}
                linkTo={APP_ROUTES.public.register}
                linkState={{ from: (location.state as LocationStateWithFrom | null)?.from }}
            />
        </AuthCard>
    );
}
