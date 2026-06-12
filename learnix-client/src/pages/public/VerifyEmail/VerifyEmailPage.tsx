import { useEffect, useState, useRef } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { CheckCircle2, XCircle, Loader2, Mail } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { authApi } from '@/api/auth.api';

type Status = 'verifying' | 'success' | 'error';

export default function VerifyEmailPage() {
    const { t } = useTranslation('emailConfirmation');
    const [searchParams] = useSearchParams();
    const [status, setStatus] = useState<Status>('verifying');
    const hasRequested = useRef(false);

    const userId = searchParams.get('userId') ?? '';
    const token = searchParams.get('token') ?? '';

    const { mutate: verify } = useMutation({
        mutationFn: () => authApi.verifyEmail({ userId, token }),
        onSuccess: () => setStatus('success'),
        meta: { suppressGlobalError: true },
        onError: () => setStatus('error'),
    });

    useEffect(() => {
        if (!userId || !token) {
            setStatus('error');
            return;
        }

        if (!hasRequested.current) {
            hasRequested.current = true;
            verify();
        }
    }, [userId, token, verify]);

    return (
        <div className="flex min-h-[60vh] items-center justify-center px-4">
            <div className="w-full max-w-md rounded-2xl border border-border bg-card p-8 text-center shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                {status === 'verifying' && (
                    <>
                        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
                            <Loader2 className="h-8 w-8 animate-spin text-primary" />
                        </div>
                        <h1 className="font-heading text-2xl font-bold text-foreground">
                            {t('verify.verifyingTitle')}
                        </h1>
                        <p className="mt-2 text-sm text-muted-foreground">
                            {t('verify.verifyingSubtitle')}
                        </p>
                    </>
                )}

                {status === 'success' && (
                    <>
                        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10">
                            <CheckCircle2 className="h-8 w-8 text-success" />
                        </div>
                        <h1 className="font-heading text-2xl font-bold text-foreground">
                            {t('verify.successTitle')}
                        </h1>
                        <p className="mt-2 text-sm text-muted-foreground">
                            {t('verify.successSubtitle')}
                        </p>
                        <Link
                            to="/login"
                            className="mt-6 inline-flex w-full items-center justify-center rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                            {t('verify.successCta')}
                        </Link>
                    </>
                )}

                {status === 'error' && (
                    <>
                        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
                            <XCircle className="h-8 w-8 text-destructive" />
                        </div>
                        <h1 className="font-heading text-2xl font-bold text-foreground">
                            {t('verify.errorTitle')}
                        </h1>
                        <p className="mt-2 text-sm text-muted-foreground">
                            {t('verify.errorSubtitle')}
                        </p>
                        <Link
                            to="/login"
                            className="mt-6 inline-flex w-full items-center justify-center rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                            {t('verify.errorCta')}
                        </Link>
                        <div className="mt-4 flex items-center justify-center gap-1.5 text-sm text-muted-foreground">
                            <Mail className="h-4 w-4" />
                            <span>{t('verify.resendHint')}</span>
                            <Link to="/login" className="font-medium text-primary hover:underline">
                                {t('verify.resend')}
                            </Link>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}
