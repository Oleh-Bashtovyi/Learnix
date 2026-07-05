import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { MailWarning } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { useEmailResendCooldown } from '@/hooks/auth/useEmailResendCooldown';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';

export function EmailConfirmationBanner() {
    const { t } = useTranslation('header');
    const user = useAuthStore((s) => s.user);
    const navigate = useNavigate();
    const location = useLocation();
    const { isCoolingDown, secondsRemaining, startCooldown } = useEmailResendCooldown();

    const mutation = useMutation({
        mutationFn: () => authApi.resendConfirmation({ email: user!.email }),
        onSuccess: () => {
            startCooldown();
            toast.success(t('resendSuccess', 'Verification email sent!'));
            navigate(APP_ROUTES.public.verifyEmail, {
                state: { email: user!.email, from: location.pathname },
            });
        },
        meta: { suppressGlobalError: true },
        onError: () => toast.error(t('resendError', 'Failed to resend. Please try again later.')),
    });

    if (!user || user.emailVerified || location.pathname === APP_ROUTES.public.verifyEmail)
        return null;

    return (
        <div className="border-b border-amber-200 bg-amber-50 dark:border-amber-900/50 dark:bg-amber-950/30">
            <div className="mx-auto flex max-w-7xl items-center gap-3.5 px-4 py-3 sm:px-6">
                <MailWarning className="size-5 shrink-0 text-amber-600 dark:text-amber-400" />
                <p className="flex-1 text-base font-medium text-amber-900 dark:text-amber-200">
                    {t('emailNotVerifiedAlert')}
                </p>
                <button
                    onClick={() => mutation.mutate()}
                    disabled={mutation.isPending || isCoolingDown}
                    className="shrink-0 rounded-md bg-amber-100 px-3.5 py-1.5 text-sm font-bold text-amber-700 transition-colors hover:bg-amber-200 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-amber-900/40 dark:text-amber-300 dark:hover:bg-amber-900/60"
                >
                    {isCoolingDown
                        ? t('resendCooldown', {
                              seconds: secondsRemaining,
                              defaultValue: `Wait ${secondsRemaining}s`,
                          })
                        : mutation.isPending
                          ? '...'
                          : t('resendEmail')}
                </button>
            </div>
        </div>
    );
}
