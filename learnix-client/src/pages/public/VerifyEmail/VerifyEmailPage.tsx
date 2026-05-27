import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { CheckCircle2, XCircle, Loader2, Mail } from 'lucide-react';
import { authApi } from '@/api/auth.api';
import { EMAIL_CONFIRMATION } from '@/const/localization/emailConfirmation';

const T = EMAIL_CONFIRMATION.VERIFY;

type Status = 'verifying' | 'success' | 'error';

export default function VerifyEmailPage() {
    const [searchParams] = useSearchParams();
    const [status, setStatus] = useState<Status>('verifying');

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
        verify();
        // run once on mount
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        <div className="flex min-h-[60vh] items-center justify-center px-4">
            <div className="w-full max-w-md rounded-2xl border border-border bg-card p-8 text-center shadow-[0_4px_20px_rgba(59,130,246,0.05)]">
                {status === 'verifying' && (
                    <>
                        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
                            <Loader2 className="h-8 w-8 animate-spin text-primary" />
                        </div>
                        <h1 className="font-heading text-2xl font-bold text-foreground">
                            {T.VERIFYING_TITLE}
                        </h1>
                        <p className="mt-2 text-sm text-muted-foreground">{T.VERIFYING_SUBTITLE}</p>
                    </>
                )}

                {status === 'success' && (
                    <>
                        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10">
                            <CheckCircle2 className="h-8 w-8 text-success" />
                        </div>
                        <h1 className="font-heading text-2xl font-bold text-foreground">
                            {T.SUCCESS_TITLE}
                        </h1>
                        <p className="mt-2 text-sm text-muted-foreground">{T.SUCCESS_SUBTITLE}</p>
                        <Link
                            to="/login"
                            className="mt-6 inline-flex w-full items-center justify-center rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                            {T.SUCCESS_CTA}
                        </Link>
                    </>
                )}

                {status === 'error' && (
                    <>
                        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
                            <XCircle className="h-8 w-8 text-destructive" />
                        </div>
                        <h1 className="font-heading text-2xl font-bold text-foreground">
                            {T.ERROR_TITLE}
                        </h1>
                        <p className="mt-2 text-sm text-muted-foreground">{T.ERROR_SUBTITLE}</p>
                        <Link
                            to="/login"
                            className="mt-6 inline-flex w-full items-center justify-center rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                            {T.ERROR_CTA}
                        </Link>
                        <div className="mt-4 flex items-center justify-center gap-1.5 text-sm text-muted-foreground">
                            <Mail className="h-4 w-4" />
                            <span>{T.RESEND_HINT}</span>
                            <Link to="/login" className="font-medium text-primary hover:underline">
                                {T.RESEND}
                            </Link>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}
