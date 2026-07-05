import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { ArrowRight, Loader2, Mail } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';
import { getRoleHome } from '@/utils/getRoleHome';
import { parseAccessToken } from '@/utils/parseAccessToken';

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-002: OTP-Based Email Verification & Auto-Login
 */
export default function VerifyEmailPage() {
    const { t } = useTranslation('auth');
    const navigate = useNavigate();
    const location = useLocation();
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);

    const from = (location.state as { from?: Location })?.from?.pathname;

    const [email, setEmail] = useState<string>((location.state as { email?: string })?.email || '');
    const [code, setCode] = useState<string[]>(Array(6).fill(''));
    const [resendCooldown, setResendCooldown] = useState(0);
    const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

    useEffect(() => {
        if (resendCooldown <= 0) return;
        const timer = setTimeout(() => setResendCooldown((c) => c - 1), 1000);
        return () => clearTimeout(timer);
    }, [resendCooldown]);

    const { mutate: verify, isPending: isVerifying } = useMutation({
        mutationFn: (tokenOverride?: string | void) =>
            authApi.verifyEmail({
                email,
                token: typeof tokenOverride === 'string' ? tokenOverride : code.join(''),
            }),
        onSuccess: (data) => {
            toast.success(t('verify.success'));
            setAccessToken(data.accessToken);
            const user = parseAccessToken(data.accessToken);
            if (user) setUser({ ...user, avatarUrl: data.avatarUrl });
            navigate(from ?? (user ? getRoleHome(user.roles) : APP_ROUTES.public.courses), {
                replace: true,
            });
        },
        onError: () => {
            toast.error(t('verify.error'));
            setCode(Array(6).fill(''));
            inputRefs.current[0]?.focus();
        },
    });

    const { mutate: resendEmail, isPending: isResending } = useMutation({
        mutationFn: () => authApi.resendConfirmation({ email }),
        onSuccess: () => {
            setResendCooldown(60);
            toast.success(
                t('verify.resendSuccess', 'Confirmation email resent. Check your inbox.'),
            );
        },
        onError: () => {
            toast.error(t('verify.resendError', 'Failed to resend. Please try again later.'));
        },
    });

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>, index: number) => {
        if (e.key === 'Backspace') {
            if (!code[index] && index > 0) {
                const newCode = [...code];
                newCode[index - 1] = '';
                setCode(newCode);
                inputRefs.current[index - 1]?.focus();
            } else {
                const newCode = [...code];
                newCode[index] = '';
                setCode(newCode);
            }
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>, index: number) => {
        const value = e.target.value.replace(/[^0-9]/g, '');
        if (!value) return;

        const newCode = [...code];
        newCode[index] = value.charAt(value.length - 1);
        setCode(newCode);

        if (index < 5 && value) {
            inputRefs.current[index + 1]?.focus();
        }

        if (index === 5 && value) {
            // Wait for state to update, then verify
            setTimeout(() => {
                const fullCode = [...newCode.slice(0, 5), value.charAt(0)].join('');
                if (fullCode.length === 6) {
                    verify(fullCode);
                }
            }, 0);
        }
    };

    const handlePaste = (e: React.ClipboardEvent) => {
        e.preventDefault();
        const pastedData = e.clipboardData
            .getData('text')
            .replace(/[^0-9]/g, '')
            .slice(0, 6);
        if (!pastedData) return;

        const newCode = [...code];
        for (let i = 0; i < pastedData.length; i++) {
            newCode[i] = pastedData[i];
        }
        setCode(newCode);

        if (pastedData.length === 6) {
            inputRefs.current[5]?.focus();
            setTimeout(() => {
                verify(pastedData);
            }, 0);
        } else {
            inputRefs.current[pastedData.length]?.focus();
        }
    };

    if (!email) {
        return (
            <div className="flex min-h-[calc(100vh-2rem)] flex-col items-center justify-center px-4 pb-20 sm:pb-28">
                <div className="w-full max-w-md rounded-2xl border border-border bg-card p-8 shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                    <h1 className="mb-4 text-center font-heading text-2xl font-bold text-foreground">
                        {t('verify.enterEmailTitle', 'Verify your email')}
                    </h1>
                    <div className="space-y-4">
                        <input
                            type="email"
                            placeholder={t('verify.emailPlaceholder', 'Email address')}
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="w-full rounded-lg border border-border bg-background px-4 py-2.5 outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
                        />
                        <button
                            onClick={() => {
                                if (!email) return;
                                resendEmail();
                            }}
                            className="w-full rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                            {t('verify.continue', 'Continue')}
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="flex min-h-[calc(100vh-2rem)] flex-col items-center justify-center px-4 pb-20 sm:pb-28">
            <div className="w-full max-w-[420px] rounded-2xl border border-border bg-card p-8 text-center shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                <div className="mx-auto mb-6 flex size-16 items-center justify-center rounded-full bg-primary/10">
                    <Mail className="size-8 text-primary" />
                </div>

                <h1 className="font-heading text-2xl font-bold text-foreground">
                    {t('verify.title', 'Check your email')}
                </h1>

                <p className="mt-2 text-sm text-muted-foreground">
                    {t('verify.subtitle', 'We sent a 6-digit verification code to')} <br />
                    <span className="font-medium text-foreground">{email}</span>
                </p>

                <div className="mt-8 flex justify-center gap-2">
                    {code.map((digit, idx) => (
                        <input
                            key={idx}
                            ref={(el) => {
                                inputRefs.current[idx] = el;
                            }}
                            type="text"
                            inputMode="numeric"
                            autoComplete="one-time-code"
                            maxLength={1}
                            value={digit}
                            onChange={(e) => handleChange(e, idx)}
                            onKeyDown={(e) => handleKeyDown(e, idx)}
                            onPaste={idx === 0 ? handlePaste : undefined}
                            disabled={isVerifying}
                            className={cn(
                                'h-14 w-12 rounded-xl border bg-background text-center text-xl font-bold outline-none transition-all',
                                'focus:border-primary focus:ring-4 focus:ring-primary/10',
                                digit
                                    ? 'border-primary text-foreground'
                                    : 'border-border text-muted-foreground',
                            )}
                        />
                    ))}
                </div>

                <button
                    onClick={() => verify()}
                    disabled={code.some((d) => !d) || isVerifying}
                    className="mt-8 flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-4 py-3.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    {isVerifying ? (
                        <Loader2 className="size-5 animate-spin" />
                    ) : (
                        <>
                            {t('verify.submit', 'Verify email')}
                            <ArrowRight className="size-4" />
                        </>
                    )}
                </button>

                <div className="mt-6 flex items-center justify-center gap-1.5 text-sm">
                    <span className="text-muted-foreground">
                        {t('verify.didntReceive', "Didn't receive the code?")}
                    </span>
                    <button
                        onClick={() => resendEmail()}
                        disabled={resendCooldown > 0 || isResending}
                        className="font-medium text-primary hover:underline disabled:cursor-not-allowed disabled:text-muted-foreground disabled:no-underline"
                    >
                        {resendCooldown > 0
                            ? t('verify.resendIn', 'Resend in {{s}}s').replace(
                                  '{{s}}',
                                  resendCooldown.toString(),
                              )
                            : isResending
                              ? '...'
                              : t('verify.resend', 'Resend')}
                    </button>
                </div>

                <div className="mt-6 border-t border-border pt-6">
                    <Link
                        to={APP_ROUTES.public.login}
                        className="text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
                    >
                        {t('verify.backToLogin', 'Back to log in')}
                    </Link>
                </div>
            </div>
        </div>
    );
}
