import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { MailWarning } from 'lucide-react';
import { useResendConfirmationEmail } from '@/hooks/auth/useResendConfirmationEmail';
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
    const {
        mutate: resend,
        isPending,
        isCoolingDown,
        secondsRemaining,
    } = useResendConfirmationEmail();

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
                    type="button"
                    onClick={() =>
                        resend(user!.email, {
                            onSuccess: () =>
                                navigate(APP_ROUTES.public.verifyEmail, {
                                    state: { email: user!.email, from: location.pathname },
                                }),
                        })
                    }
                    disabled={isPending || isCoolingDown}
                    className="shrink-0 rounded-md bg-warning/20 px-3.5 py-1.5 text-sm font-bold text-warning transition-colors hover:bg-warning/30 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {isCoolingDown
                        ? t('resendCooldown', { seconds: secondsRemaining })
                        : isPending
                          ? '...'
                          : t('resendEmail')}
                </button>
            </div>
        </div>
    );
}
