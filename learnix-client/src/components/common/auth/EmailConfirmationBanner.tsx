import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { MailWarning } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { useEmailResendCooldown } from '@/hooks/auth/useEmailResendCooldown';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-007: Persistent Email-Confirmation Banner Gated on the JWT Claim
 */
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
        <div className="border-b border-warning/30 bg-warning/10">
            <div className="mx-auto flex max-w-7xl items-center gap-3.5 px-4 py-3 sm:px-6">
                <MailWarning className="size-5 shrink-0 text-warning" />
                <p className="flex-1 text-base font-medium text-warning">
                    {t('emailNotVerifiedAlert')}
                </p>
                <button
                    onClick={() => mutation.mutate()}
                    disabled={mutation.isPending || isCoolingDown}
                    className="shrink-0 rounded-md bg-warning/20 px-3.5 py-1.5 text-sm font-bold text-warning transition-colors hover:bg-warning/30 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {isCoolingDown
                        ? t('resendCooldown', { seconds: secondsRemaining })
                        : mutation.isPending
                          ? '...'
                          : t('resendEmail')}
                </button>
            </div>
        </div>
    );
}
