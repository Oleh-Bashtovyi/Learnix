import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { Eye, EyeOff } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { loginSchema, type LoginFormData } from '@/schemas/auth.schema';
import { useAuthStore } from '@/store/auth.store';
import { AUTH_PAGES } from '@/const/localization/authPages';
import { isValidationError, setApiFieldErrors } from '@/utils/errors';
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
    const [showPassword, setShowPassword] = useState(false);

    const from = (location.state as { from?: Location })?.from?.pathname ?? '/dashboard';

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

    const onSubmit = async (data: LoginFormData) => {
        try {
            const response = await mutateAsync(data);
            setAccessToken(response.accessToken);
            toast.success(T.successRedirect);
            navigate(from, { replace: true });
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
        <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center px-4 py-12">
            <div className="w-full max-w-[420px]">
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
                        <h1 className="font-heading text-2xl font-bold text-foreground">
                            {T.title}
                        </h1>
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

                    <button
                        type="button"
                        onClick={() => toast.info('Google login coming soon!')}
                        className="flex w-full items-center justify-center gap-2.5 rounded-lg border border-border bg-background px-4 py-2.5 text-sm font-medium text-foreground transition-colors hover:bg-secondary"
                    >
                        <GoogleIcon />
                        {T.google}
                    </button>

                    <p className="mt-6 text-center text-sm text-muted-foreground">
                        {T.noAccount}{' '}
                        <Link to="/register" className="font-medium text-primary hover:underline">
                            {T.register}
                        </Link>
                    </p>
                </div>
            </div>
        </div>
    );
}

function GoogleIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true">
            <path
                d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                fill="#4285F4"
            />
            <path
                d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                fill="#34A853"
            />
            <path
                d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                fill="#FBBC05"
            />
            <path
                d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                fill="#EA4335"
            />
        </svg>
    );
}
