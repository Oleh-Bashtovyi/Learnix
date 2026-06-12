import { useParams, Link } from 'react-router-dom';
import { ShieldCheck, XCircle, Download, Calendar, User, BookOpen, Clock } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useVerifyCertificate } from '@/hooks/useVerifyCertificate';
import { cn } from '@/utils/cn';

export default function CertificateVerifyPage() {
    const { code } = useParams<{ code: string }>();
    const { t } = useTranslation('certificates');
    const { data: certificate, isLoading, isError } = useVerifyCertificate(code ?? '');

    if (isLoading) {
        return (
            <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center p-4">
                <div className="flex flex-col items-center gap-4">
                    <div className="h-10 w-10 animate-spin rounded-full border-4 border-primary border-t-transparent" />
                    <p className="text-muted-foreground">
                        {t('verifying', { defaultValue: 'Verifying certificate...' })}
                    </p>
                </div>
            </div>
        );
    }

    if (isError || !certificate) {
        return (
            <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center p-4">
                <div className="mx-auto w-full max-w-md rounded-2xl border border-border bg-card p-8 text-center shadow-lg">
                    <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
                        <XCircle className="h-8 w-8 text-destructive" />
                    </div>
                    <h1 className="mt-6 font-heading text-2xl font-bold text-foreground">
                        {t('verify.invalidTitle', { defaultValue: 'Invalid Certificate' })}
                    </h1>
                    <p className="mt-2 text-muted-foreground">
                        {t('verify.invalidDesc', {
                            defaultValue:
                                'We could not find a valid certificate matching this code. Please check the URL and try again.',
                        })}
                    </p>
                    <div className="mt-8">
                        <Link
                            to="/"
                            className="inline-flex items-center justify-center rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                            {t('verify.backHome', { defaultValue: 'Return to Homepage' })}
                        </Link>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center bg-muted/30 p-4">
            <div className="mx-auto w-full max-w-2xl">
                <div className="relative overflow-hidden rounded-2xl border border-border bg-card p-6 shadow-xl sm:p-10">
                    {/* Decorative Background */}
                    <div className="pointer-events-none absolute right-0 top-0 -mr-16 -mt-16 h-64 w-64 rounded-full bg-success/5 blur-3xl" />

                    <div className="relative border-b border-border pb-8 text-center">
                        <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-success/15 ring-8 ring-success/5">
                            <ShieldCheck className="h-10 w-10 text-success" />
                        </div>
                        <h1 className="mb-2 font-heading text-2xl font-bold text-foreground sm:text-3xl">
                            {t('verify.validTitle', { defaultValue: 'Certificate Verified' })}
                        </h1>
                        <p className="mx-auto max-w-md text-sm text-muted-foreground sm:text-base">
                            {t('verify.validDesc', {
                                defaultValue: 'This is a valid certificate issued by Learnix.',
                            })}
                        </p>
                    </div>

                    <div className="space-y-6 py-8">
                        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                            <div className="space-y-1">
                                <div className="mb-1 flex items-center gap-2 text-sm text-muted-foreground">
                                    <User className="h-4 w-4" />
                                    <span>
                                        {t('verify.student', { defaultValue: 'Awarded to' })}
                                    </span>
                                </div>
                                <p className="text-lg font-semibold text-foreground">
                                    {certificate.studentFirstName} {certificate.studentLastName}
                                </p>
                            </div>

                            <div className="space-y-1">
                                <div className="mb-1 flex items-center gap-2 text-sm text-muted-foreground">
                                    <BookOpen className="h-4 w-4" />
                                    <span>
                                        {t('verify.course', {
                                            defaultValue: 'For successful completion of',
                                        })}
                                    </span>
                                </div>
                                <p className="text-lg font-semibold text-foreground">
                                    {certificate.courseTitle}
                                </p>
                            </div>

                            <div className="space-y-1">
                                <div className="mb-1 flex items-center gap-2 text-sm text-muted-foreground">
                                    <User className="h-4 w-4" />
                                    <span>
                                        {t('verify.instructor', { defaultValue: 'Instructor' })}
                                    </span>
                                </div>
                                <p className="font-medium text-foreground">
                                    {certificate.instructorFirstName}{' '}
                                    {certificate.instructorLastName}
                                </p>
                            </div>

                            <div className="space-y-1">
                                <div className="mb-1 flex items-center gap-2 text-sm text-muted-foreground">
                                    <Calendar className="h-4 w-4" />
                                    <span>
                                        {t('verify.issuedAt', { defaultValue: 'Issued on' })}
                                    </span>
                                </div>
                                <p className="font-medium text-foreground">
                                    {new Date(certificate.issuedAt).toLocaleDateString(undefined, {
                                        year: 'numeric',
                                        month: 'long',
                                        day: 'numeric',
                                    })}
                                </p>
                            </div>
                        </div>

                        <div className="flex items-center justify-between border-t border-border pt-6">
                            <div className="text-sm">
                                <span className="text-muted-foreground">
                                    {t('verify.certificateId', { defaultValue: 'Certificate ID' })}
                                    :{' '}
                                </span>
                                <span className="font-mono font-medium text-foreground">
                                    {certificate.code}
                                </span>
                            </div>
                        </div>
                    </div>

                    <div className="mt-4 flex flex-col justify-center gap-4 sm:flex-row">
                        {certificate.isReady && certificate.downloadUrl ? (
                            <a
                                href={certificate.downloadUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                            >
                                <Download className="h-4 w-4" />
                                {t('actions.download', { defaultValue: 'Download PDF' })}
                            </a>
                        ) : (
                            <span className="inline-flex cursor-not-allowed items-center justify-center gap-2 rounded-lg bg-muted px-6 py-2.5 text-sm text-muted-foreground">
                                <Clock className="h-4 w-4" />
                                {t('status.generating', { defaultValue: 'Generating PDF...' })}
                            </span>
                        )}
                        <Link
                            to="/"
                            className="inline-flex items-center justify-center gap-2 rounded-lg border border-border bg-transparent px-6 py-2.5 text-sm font-medium text-foreground transition-colors hover:bg-secondary"
                        >
                            {t('verify.backHome', { defaultValue: 'Return to Homepage' })}
                        </Link>
                    </div>
                </div>
            </div>
        </div>
    );
}
