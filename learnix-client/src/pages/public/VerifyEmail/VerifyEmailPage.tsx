import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { ArrowRight, Mail } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { AuthCard } from '@/components/common/auth/AuthCard';
import { AuthFooter } from '@/components/common/auth/AuthFooter';
import { FormInput } from '@/components/common/form/FormInput';
import { AsyncButton } from '@/components/ui/async-button';
import { useResendConfirmationEmail } from '@/hooks/auth/useResendConfirmationEmail';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';
import { getRoleHome } from '@/utils/getRoleHome';
import { parseAccessToken } from '@/utils/parseAccessToken';

const CODE_LENGTH = 6;

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
    const user = useAuthStore((s) => s.user);

    const from = (location.state as { from?: Location })?.from?.pathname;

    const [email, setEmail] = useState<string>((location.state as { email?: string })?.email || '');
    // Kept apart from `email`, which decides *which* screen shows: typing into a field bound to it
    // would swap the screen on the first keystroke, one character into the address.
    const [emailInput, setEmailInput] = useState('');
    const [code, setCode] = useState<string[]>(new Array(CODE_LENGTH).fill(''));
    const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

    const {
        mutate: verify,
        isPending: isVerifying,
        isSuccess: isVerifySuccess,
    } = useMutation({
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
            setCode(new Array(CODE_LENGTH).fill(''));
            inputRefs.current[0]?.focus();
        },
    });

    const {
        mutate: resendEmail,
        isPending: isResending,
        isSuccess: isResendSuccess,
        isCoolingDown,
        secondsRemaining,
    } = useResendConfirmationEmail();

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

    /**
     * Spreads however many digits arrived across the boxes from `index` onwards, and submits once six
     * are in. Both paths lead here: a desktop paste fires `paste` with the whole string, while phone
     * keyboards routinely deliver a paste as a plain `change` with all six digits in one box — reading
     * a single character there is why pasting used to fill the first square and stop.
     */
    const fillFrom = (index: number, digits: string) => {
        if (!digits) return;

        const newCode = [...code];
        for (let i = 0; i < digits.length && index + i < CODE_LENGTH; i++) {
            newCode[index + i] = digits[i];
        }
        setCode(newCode);

        const filledTo = Math.min(index + digits.length, CODE_LENGTH - 1);
        inputRefs.current[filledTo]?.focus();

        if (newCode.every((digit) => digit !== '')) {
            // Let the state land before the request reads it.
            setTimeout(() => verify(newCode.join('')), 0);
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>, index: number) => {
        const value = e.target.value.replace(/\D/g, '');
        if (!value) return;

        // A single keystroke into a filled box replaces it; anything longer is a paste in disguise.
        fillFrom(index, value.length === 1 ? value : value.slice(0, CODE_LENGTH - index));
    };

    const handlePaste = (e: React.ClipboardEvent, index: number) => {
        e.preventDefault();
        const pasted = e.clipboardData
            .getData('text')
            .replace(/\D/g, '')
            .slice(0, CODE_LENGTH - index);
        fillFrom(index, pasted);
    };

    if (!email) {
        return (
            <div className="flex w-full flex-1 items-center justify-center px-4 py-12 md:py-24">
                <AuthCard className="max-w-[420px]">
                    <h1 className="mb-4 text-center font-heading text-2xl font-bold text-foreground">
                        {t('verify.enterEmailTitle', 'Verify your email')}
                    </h1>
                    <div className="space-y-4">
                        <FormInput
                            type="email"
                            placeholder={t('verify.emailPlaceholder', 'Email address')}
                            value={emailInput}
                            onChange={(e) => setEmailInput(e.target.value)}
                        />
                        <AsyncButton
                            onClick={() => {
                                const value = emailInput.trim();
                                if (!value) return;
                                setEmail(value);
                                resendEmail(value);
                            }}
                            isLoading={isResending}
                            isSuccess={isResendSuccess}
                            className="w-full rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold transition-colors hover:bg-primary/90"
                        >
                            {t('verify.continue', 'Continue')}
                        </AsyncButton>
                    </div>
                </AuthCard>
            </div>
        );
    }

    return (
        <div className="flex w-full flex-1 items-center justify-center px-4 py-12 md:py-24">
            <AuthCard className="max-w-[420px] text-center">
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

                {/* The boxes shrink with the card instead of keeping a fixed 48px: six of those plus the
                    gaps and the card's padding need ~400px of viewport, so on a 360px phone the last
                    one fell off the edge. */}
                <div className="mt-8 flex justify-center gap-1.5 sm:gap-2">
                    {code.map((digit, idx) => (
                        <input
                            key={idx}
                            ref={(el) => {
                                inputRefs.current[idx] = el;
                            }}
                            type="text"
                            inputMode="numeric"
                            autoComplete="one-time-code"
                            // Not 1: with maxLength={1} the browser truncates a pasted code to a single
                            // character before onChange ever sees it. The box still shows one digit —
                            // the value is controlled by state — but a paste now arrives whole.
                            maxLength={CODE_LENGTH}
                            value={digit}
                            onChange={(e) => handleChange(e, idx)}
                            onKeyDown={(e) => handleKeyDown(e, idx)}
                            onPaste={(e) => handlePaste(e, idx)}
                            disabled={isVerifying}
                            className={cn(
                                'h-14 w-full min-w-0 max-w-12 flex-1 rounded-xl border bg-background text-center text-xl font-bold outline-none transition-all',
                                'focus:border-primary focus:ring-4 focus:ring-primary/10',
                                digit
                                    ? 'border-primary text-foreground'
                                    : 'border-border text-muted-foreground',
                            )}
                        />
                    ))}
                </div>

                <AsyncButton
                    onClick={() => verify()}
                    disabled={code.some((d) => !d) || isVerifying}
                    isLoading={isVerifying}
                    isSuccess={isVerifySuccess}
                    className="mt-8 flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-4 py-3.5 text-sm font-semibold transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    {t('verify.submit', 'Verify email')}
                    <ArrowRight className="size-4" />
                </AsyncButton>

                <div className="mt-6 flex items-center justify-center gap-1.5 text-sm">
                    <span className="text-muted-foreground">
                        {t('verify.didntReceive', "Didn't receive the code?")}
                    </span>
                    <button
                        onClick={() => resendEmail(email)}
                        disabled={isCoolingDown || isResending}
                        className="font-medium text-primary hover:underline disabled:cursor-not-allowed disabled:text-muted-foreground disabled:no-underline"
                    >
                        {isCoolingDown
                            ? t('verify.resendIn', 'Resend in {{s}}s').replace(
                                  '{{s}}',
                                  secondsRemaining.toString(),
                              )
                            : isResending
                              ? '...'
                              : t('verify.resend', 'Resend')}
                    </button>
                </div>

                {!user && (
                    <AuthFooter
                        linkText={t('verify.backToLogin', 'Back to log in')}
                        linkTo={APP_ROUTES.public.login}
                    />
                )}
            </AuthCard>
        </div>
    );
}
