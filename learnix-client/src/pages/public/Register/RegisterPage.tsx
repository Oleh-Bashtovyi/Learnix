import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { GoogleLogin } from '@react-oauth/google';
import { useMutation } from '@tanstack/react-query';
import { CheckCircle2, Circle } from 'lucide-react';
import { toast } from 'sonner';
import { type RegisterRequest, authApi } from '@/api/auth.api';
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
import { type RegisterFormData, registerSchema } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import type { LocationStateWithFrom } from '@/types/router.types';
import { cn } from '@/utils/cn';
import { type ApiFieldMap, handleFormError } from '@/utils/errors';
import { parseAccessToken } from '@/utils/parseAccessToken';

const REGISTER_FIELD_MAP: ApiFieldMap<RegisterRequest, RegisterFormData> = {
    email: 'email',
    password: 'password',
    firstName: 'firstName',
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
 * - ADR-FRONT-FORMS-005: Centralized Form Error Handling
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
            handleFormError(err, setError, t('register.errors.fallback'), REGISTER_FIELD_MAP);
        }
    };

    return (
        <AuthCard className="max-w-[480px]">
            <AuthHeader title={t('register.title')} subtitle={t('register.subtitle')} />

            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
                <FormErrorAlert message={errors.root?.message} />

                <div className="grid grid-cols-2 gap-4">
                    <FormInput
                        id="firstName"
                        autoComplete="given-name"
                        variant="auth"
                        label={t('common:general.firstName')}
                        placeholder={t('register.firstName.placeholder')}
                        error={errors.firstName}
                        maxLength={AUTH_LIMITS.FIRST_NAME_MAX}
                        {...register('firstName')}
                    />

                    <FormInput
                        id="lastName"
                        autoComplete="family-name"
                        variant="auth"
                        label={t('common:general.lastName')}
                        placeholder={t('register.lastName.placeholder')}
                        error={errors.lastName}
                        maxLength={AUTH_LIMITS.LAST_NAME_MAX}
                        {...register('lastName')}
                    />
                </div>

                <FormInput
                    id="email"
                    type="email"
                    autoComplete="email"
                    variant="auth"
                    label={t('register.email.label')}
                    placeholder={t('register.email.placeholder')}
                    error={errors.email}
                    maxLength={AUTH_LIMITS.EMAIL_MAX}
                    {...register('email')}
                />

                <div>
                    <PasswordInput
                        id="password"
                        autoComplete="new-password"
                        variant="auth"
                        label={t('register.password.label')}
                        placeholder={t('register.password.placeholder')}
                        error={errors.password}
                        maxLength={AUTH_LIMITS.PASSWORD_MAX}
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
                    variant="auth"
                    label={t('register.confirmPassword.label')}
                    placeholder={t('register.confirmPassword.placeholder')}
                    error={errors.confirmPassword}
                    maxLength={AUTH_LIMITS.PASSWORD_MAX}
                    {...register('confirmPassword')}
                />

                <Button type="submit" disabled={isSubmitting} className="mt-2 w-full">
                    {isSubmitting ? t('register.submitting') : t('register.submit')}
                </Button>
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

            <AuthFooter
                text={t('register.hasAccount')}
                linkText={t('common:actions.logIn')}
                linkTo={APP_ROUTES.public.login}
                linkState={{ from: (location.state as LocationStateWithFrom | null)?.from }}
            />
        </AuthCard>
    );
}
