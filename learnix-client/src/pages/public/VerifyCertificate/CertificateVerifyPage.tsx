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
                    <p className="text-muted-foreground">{t('verifying', { defaultValue: 'Verifying certificate...' })}</p>
                </div>
            </div>
        );
    }

    if (isError || !certificate) {
        return (
            <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center p-4">
                <div className="mx-auto max-w-md w-full rounded-2xl border border-border bg-card p-8 text-center shadow-lg">
                    <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
                        <XCircle className="h-8 w-8 text-destructive" />
                    </div>
                    <h1 className="mt-6 font-heading text-2xl font-bold text-foreground">
                        {t('verify.invalidTitle', { defaultValue: 'Invalid Certificate' })}
                    </h1>
                    <p className="mt-2 text-muted-foreground">
                        {t('verify.invalidDesc', { defaultValue: 'We could not find a valid certificate matching this code. Please check the URL and try again.' })}
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
        <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center p-4 bg-muted/30">
            <div className="mx-auto max-w-2xl w-full">
                <div className="rounded-2xl border border-border bg-card p-6 sm:p-10 shadow-xl relative overflow-hidden">
                    {/* Decorative Background */}
                    <div className="absolute top-0 right-0 -mr-16 -mt-16 w-64 h-64 bg-success/5 rounded-full blur-3xl pointer-events-none" />

                    <div className="relative text-center pb-8 border-b border-border">
                        <div className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-success/15 mb-6 ring-8 ring-success/5">
                            <ShieldCheck className="h-10 w-10 text-success" />
                        </div>
                        <h1 className="font-heading text-2xl sm:text-3xl font-bold text-foreground mb-2">
                            {t('verify.validTitle', { defaultValue: 'Certificate Verified' })}
                        </h1>
                        <p className="text-muted-foreground text-sm sm:text-base max-w-md mx-auto">
                            {t('verify.validDesc', { defaultValue: 'This is a valid certificate issued by Learnix.' })}
                        </p>
                    </div>

                    <div className="py-8 space-y-6">
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                            <div className="space-y-1">
                                <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
                                    <User className="h-4 w-4" />
                                    <span>{t('verify.student', { defaultValue: 'Awarded to' })}</span>
                                </div>
                                <p className="font-semibold text-lg text-foreground">
                                    {certificate.studentFirstName} {certificate.studentLastName}
                                </p>
                            </div>

                            <div className="space-y-1">
                                <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
                                    <BookOpen className="h-4 w-4" />
                                    <span>{t('verify.course', { defaultValue: 'For successful completion of' })}</span>
                                </div>
                                <p className="font-semibold text-lg text-foreground">
                                    {certificate.courseTitle}
                                </p>
                            </div>

                            <div className="space-y-1">
                                <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
                                    <User className="h-4 w-4" />
                                    <span>{t('verify.instructor', { defaultValue: 'Instructor' })}</span>
                                </div>
                                <p className="font-medium text-foreground">
                                    {certificate.instructorFirstName} {certificate.instructorLastName}
                                </p>
                            </div>

                            <div className="space-y-1">
                                <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
                                    <Calendar className="h-4 w-4" />
                                    <span>{t('verify.issuedAt', { defaultValue: 'Issued on' })}</span>
                                </div>
                                <p className="font-medium text-foreground">
                                    {new Date(certificate.issuedAt).toLocaleDateString(undefined, {
                                        year: 'numeric',
                                        month: 'long',
                                        day: 'numeric'
                                    })}
                                </p>
                            </div>
                        </div>

                        <div className="pt-6 border-t border-border flex items-center justify-between">
                            <div className="text-sm">
                                <span className="text-muted-foreground">{t('verify.certificateId', { defaultValue: 'Certificate ID' })}: </span>
                                <span className="font-mono text-foreground font-medium">{certificate.code}</span>
                            </div>
                        </div>
                    </div>

                    <div className="flex flex-col sm:flex-row gap-4 justify-center mt-4">
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
                            <span className="inline-flex items-center justify-center gap-2 rounded-lg bg-muted px-6 py-2.5 text-sm text-muted-foreground cursor-not-allowed">
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
