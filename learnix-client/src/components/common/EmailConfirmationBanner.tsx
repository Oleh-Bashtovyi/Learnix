import { useState, useEffect } from 'react';
import { MailWarning } from 'lucide-react';
import { toast } from 'sonner';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/store/auth.store';
import { authApi } from '@/api/auth.api';

export function EmailConfirmationBanner() {
    const { t } = useTranslation('emailConfirmation');
    const user = useAuthStore((s) => s.user);
    const [cooldown, setCooldown] = useState(0);

    useEffect(() => {
        if (cooldown <= 0) return;
        const timer = setTimeout(() => setCooldown((c) => c - 1), 1000);
        return () => clearTimeout(timer);
    }, [cooldown]);

    const { mutate: resend, isPending } = useMutation({
        mutationFn: () => authApi.resendConfirmation({ email: user!.email }),
        onSuccess: () => {
            setCooldown(60);
            toast.success('Confirmation email sent. Check your inbox.');
        },
        meta: { suppressGlobalError: true },
        onError: () => toast.error('Failed to resend. Please try again later.'),
    });

    if (!user || user.emailVerified) return null;

    return (
        <div className="border-b border-amber-200 bg-amber-50 dark:border-amber-900/50 dark:bg-amber-950/30">
            <div className="mx-auto flex max-w-7xl items-center gap-3 px-4 py-2.5 sm:px-6">
                <MailWarning className="h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400" />
                <p className="flex-1 text-sm text-amber-800 dark:text-amber-300">
                    {t('banner.message')}
                </p>
                <button
                    onClick={() => resend()}
                    disabled={cooldown > 0 || isPending}
                    className="shrink-0 text-sm font-medium text-amber-700 underline-offset-2 hover:underline disabled:cursor-not-allowed disabled:opacity-60 dark:text-amber-400"
                >
                    {cooldown > 0
                        ? t('banner.resendCooldown', { seconds: cooldown })
                        : t('banner.resend')}
                </button>
            </div>
        </div>
    );
}
