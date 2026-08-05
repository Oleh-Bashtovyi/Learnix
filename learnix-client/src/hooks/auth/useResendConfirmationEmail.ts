import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { useEmailResendCooldown } from '@/hooks/auth/useEmailResendCooldown';

/**
 * Resends the OTP confirmation email and starts the shared, localStorage-persisted cooldown
 * (`useEmailResendCooldown`) — the same cooldown regardless of which surface triggered it, so it
 * survives a reload or navigating between the profile page, the header banner and the verify page.
 *
 * Toast + cooldown are the only side effects owned here. A caller that needs to do something more
 * on success (the banner and the profile page navigate to the verify-email screen) passes its own
 * `onSuccess` to `mutate()` — TanStack Query runs both, this hook's first.
 */
export function useResendConfirmationEmail() {
    const { t } = useTranslation('auth');
    const { isCoolingDown, secondsRemaining, startCooldown } = useEmailResendCooldown();

    const mutation = useMutation({
        mutationFn: (email: string) => authApi.resendConfirmation({ email }),
        onSuccess: () => {
            startCooldown();
            toast.success(t('verify.resendSuccess'));
        },
        meta: { suppressGlobalError: true },
        onError: () => toast.error(t('verify.resendError')),
    });

    return { ...mutation, isCoolingDown, secondsRemaining };
}
