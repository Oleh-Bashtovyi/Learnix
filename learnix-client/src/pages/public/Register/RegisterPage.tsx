import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { CheckCircle2, Circle } from 'lucide-react';
import { toast } from 'sonner';
import { type RegisterRequest, authApi } from '@/api/auth.api';
import { AuthCard } from '@/components/common/auth/AuthCard';
import { AuthDivider } from '@/components/common/auth/AuthDivider';
import { AuthFooter } from '@/components/common/auth/AuthFooter';
import { AuthHeader } from '@/components/common/auth/AuthHeader';
import { GoogleAuthButton } from '@/components/common/auth/GoogleAuthButton';
import { FormErrorAlert } from '@/components/common/form/FormErrorAlert';
import { FormInput } from '@/components/common/form/FormInput';
import { PasswordInput } from '@/components/common/form/PasswordInput';
import { AsyncButton } from '@/components/ui/async-button';
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
    isSubmitted: boolean;
};

function PasswordRule({ met, label, isSubmitted }: PasswordRuleProps) {
    const textColor = met
        ? 'text-success'
        : isSubmitted
          ? 'text-destructive'
          : 'text-muted-foreground';

    return (
        <li className={cn('flex items-center gap-1.5 text-xs', textColor)}>
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
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);

    const {
        register,
        handleSubmit,
        control,
        setError,
        formState: { errors, isSubmitting, isSubmitSuccessful, isSubmitted },
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

            // Set auth state immediately. RequireGuest will detect the unverified user
            // and automatically redirect them to /verify-email.
            setAccessToken(response.accessToken);
            const user = parseAccessToken(response.accessToken);
            if (user) setUser({ ...user, avatarUrl: response.avatarUrl });
        } catch (err) {
            handleFormError(err, setError, t('register.errors.fallback'), REGISTER_FIELD_MAP);
        }
    };

    return (
        <AuthCard className="max-w-[480px]">
            <AuthHeader title={t('register.title')} subtitle={t('register.subtitle')} />

            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-1">
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

                <div className="pb-4">
                    <PasswordInput
                        id="password"
                        autoComplete="new-password"
                        variant="auth"
                        label={t('register.password.label')}
                        placeholder={t('register.password.placeholder')}
                        error={errors.password}
                        hideErrorText={true}
                        maxLength={AUTH_LIMITS.PASSWORD_MAX}
                        {...register('password')}
                    />
                    <ul className="mt-3 space-y-1.5 pl-1">
                        <PasswordRule
                            met={passwordRules.minLength}
                            label={t('register.password.requirements.minLength')}
                            isSubmitted={isSubmitted}
                        />
                        <PasswordRule
                            met={passwordRules.uppercase}
                            label={t('register.password.requirements.uppercase')}
                            isSubmitted={isSubmitted}
                        />
                        <PasswordRule
                            met={passwordRules.lowercase}
                            label={t('register.password.requirements.lowercase')}
                            isSubmitted={isSubmitted}
                        />
                        <PasswordRule
                            met={passwordRules.digit}
                            label={t('register.password.requirements.digit')}
                            isSubmitted={isSubmitted}
                        />
                    </ul>
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

                <AsyncButton
                    type="submit"
                    disabled={isSubmitting}
                    isLoading={isSubmitting}
                    isSuccess={isSubmitSuccessful}
                    loadingText={t('register.submitting')}
                    className="mt-2 w-full"
                >
                    {t('register.submit')}
                </AsyncButton>
            </form>

            <AuthDivider text={t('register.divider')} />

            <GoogleAuthButton
                text={t('login.google')}
                onSuccess={onGoogleCredential}
                onError={() => toast.error(t('register.googleError'))}
            />

            <AuthFooter
                text={t('register.hasAccount')}
                linkText={t('common:actions.logIn')}
                linkTo={APP_ROUTES.public.login}
                linkState={{ from: (location.state as LocationStateWithFrom | null)?.from }}
            />
        </AuthCard>
    );
}
