import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { GoogleLogin } from '@react-oauth/google';
import { Eye, EyeOff } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { loginSchema, type LoginFormData } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import { useGoogleAuth } from '@/hooks/useGoogleAuth';
import { AUTH_PAGES } from '@/const/localization/authPages';
import { isValidationError, setApiFieldErrors } from '@/utils/errors';
import { parseAccessToken } from '@/utils/parseAccessToken';
import { getRoleHome } from '@/utils/getRoleHome';
import { cn } from '@/utils/cn';

const T = AUTH_PAGES.LOGIN;

const LOGIN_FIELD_MAP: Partial<Record<string, keyof LoginFormData>> = {
    Email: 'email',
    email: 'email',
    Password: 'password',
    password: 'password',
};

export default function LoginPage() {
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
        formState: { errors, isSubmitting },
    } = useForm<LoginFormData>({
        resolver: zodResolver(loginSchema),
    });

    const { mutateAsync } = useMutation({
        mutationFn: authApi.login,
    });

    const { onGoogleCredential } = useGoogleAuth();

    const onSubmit = async (data: LoginFormData) => {
        try {
            const response = await mutateAsync(data);
            setAccessToken(response.accessToken);
            const user = parseAccessToken(response.accessToken);
            if (user) setUser(user);
            toast.success(T.successRedirect);
            navigate(from ?? (user ? getRoleHome(user.role) : '/courses'), { replace: true });
        } catch (err) {
            if (isValidationError(err)) {
                setApiFieldErrors(err, setError, LOGIN_FIELD_MAP);
            } else {
                setError('root', {
                    type: 'server',
                    message: err instanceof Error ? err.message : 'Login failed. Please try again.',
                });
            }
        }
    };

    return (
        <div className="w-full max-w-[420px] py-12">
            <div className="rounded-2xl border border-border bg-card p-8 shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                <div className="mb-8 text-center">
                    <Link
                        to="/"
                        className="mb-6 inline-flex items-center gap-2 font-heading font-bold"
                    >
                        <div className="grid h-9 w-9 place-items-center rounded-lg bg-primary font-heading text-lg font-bold text-primary-foreground">
                            L
                        </div>
                        <span className="text-xl">Learnix</span>
                    </Link>
                    <h1 className="font-heading text-2xl font-bold text-foreground">{T.title}</h1>
                    <p className="mt-1 text-sm text-muted-foreground">{T.subtitle}</p>
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
                            {T.email.label}
                        </label>
                        <input
                            id="email"
                            type="email"
                            autoComplete="email"
                            placeholder={T.email.placeholder}
                            {...register('email')}
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
                                {T.password.label}
                            </label>
                            <Link
                                to="/forgot-password"
                                className="text-xs text-primary hover:underline"
                            >
                                {T.forgotPassword}
                            </Link>
                        </div>
                        <div className="relative">
                            <input
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                autoComplete="current-password"
                                placeholder={T.password.placeholder}
                                {...register('password')}
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
                                    <EyeOff className="h-4 w-4" />
                                ) : (
                                    <Eye className="h-4 w-4" />
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
                        {isSubmitting ? T.submitting : T.submit}
                    </button>
                </form>

                <div className="my-6 flex items-center gap-3">
                    <div className="h-px flex-1 bg-border" />
                    <span className="text-xs text-muted-foreground">{T.divider}</span>
                    <div className="h-px flex-1 bg-border" />
                </div>

                <div className="flex justify-center">
                    <GoogleLogin
                        onSuccess={(response) => {
                            if (response.credential) {
                                onGoogleCredential(response.credential);
                            }
                        }}
                        onError={() => toast.error(T.googleError)}
                        theme="outline"
                        size="large"
                        shape="rectangular"
                        text="continue_with"
                        width={356}
                    />
                </div>

                <p className="mt-6 text-center text-sm text-muted-foreground">
                    {T.noAccount}{' '}
                    <Link to="/register" className="font-medium text-primary hover:underline">
                        {T.register}
                    </Link>
                </p>
            </div>
        </div>
    );
}
