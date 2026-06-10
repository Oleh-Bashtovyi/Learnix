import { GraduationCap, Download, Link as LinkIcon, Clock } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { useMyCertificates } from '@/hooks/useMyCertificates';
import { cn } from '@/utils/cn';

export default function CertificatesPage() {
    const { t } = useTranslation('certificates');
    const { data: certificates, isLoading } = useMyCertificates();

    function copyLink(url: string) {
        navigator.clipboard.writeText(url);
        toast.success(t('messages.linkCopied'));
    }

    if (isLoading) {
        return (
            <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 sm:py-10">
                <div className="animate-pulse space-y-4">
                    <div className="h-8 w-56 rounded bg-muted" />
                    {Array.from({ length: 3 }).map((_, i) => (
                        <div key={i} className="h-24 max-w-4xl rounded-xl bg-muted" />
                    ))}
                </div>
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 sm:py-10">

            {!certificates || certificates.length === 0 ? (
                <div className="mt-16 text-center">
                    <div className="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-accent/10">
                        <GraduationCap className="h-12 w-12 text-accent" />
                    </div>
                    <h2 className="mt-6 font-heading text-2xl font-bold">{t('emptyTitle')}</h2>
                    <p className="mt-2 text-muted-foreground">{t('emptyDescription')}</p>
                </div>
            ) : (
                <div className="mt-6 sm:mt-8 space-y-4 max-w-4xl">
                    {certificates.map((cert) => (
                        <div
                            key={cert.certificateId}
                            className="flex flex-col sm:flex-row sm:items-center gap-4 sm:gap-5 rounded-xl border border-border bg-card p-4 sm:p-5 transition-shadow hover:shadow-sm"
                        >
                            {/* Info */}
                            <div className="flex items-center gap-3 sm:gap-5 flex-1 min-w-0">
                                <div
                                    className={cn(
                                        'flex h-12 w-12 sm:h-16 sm:w-16 shrink-0 items-center justify-center rounded-lg',
                                        cert.courseCoverBlobPath ? 'overflow-hidden' : 'bg-accent/10',
                                    )}
                                >
                                    {cert.courseCoverBlobPath ? (
                                        <img
                                            src={cert.courseCoverBlobPath}
                                            alt={cert.courseTitle}
                                            className="h-full w-full object-cover"
                                        />
                                    ) : (
                                        <GraduationCap className="h-6 w-6 sm:h-8 sm:w-8 text-accent" />
                                    )}
                                </div>
                                <div className="min-w-0 flex-1">
                                    <p className="line-clamp-2 sm:line-clamp-1 font-heading text-sm sm:text-base font-semibold text-foreground">
                                        {cert.courseTitle}
                                </p>
                                <p className="mt-0.5 text-sm text-muted-foreground">
                                    {t('issuedOn')}{' '}
                                    {new Date(cert.issuedAt).toLocaleDateString('en-US', {
                                        month: 'long',
                                        day: 'numeric',
                                        year: 'numeric',
                                    })}
                                </p>
                                {!cert.isReady && (
                                    <p className="mt-1 flex items-center gap-1 text-xs text-warning">
                                        <Clock className="h-3 w-3" />
                                        {t('status.generatingHint')}
                                    </p>
                                )}
                                </div>
                            </div>

                            {/* Actions */}
                            <div className="flex shrink-0 items-center gap-2 justify-end sm:justify-start w-full sm:w-auto">
                                <button
                                    type="button"
                                    onClick={() => copyLink(cert.verificationUrl)}
                                    title={t('actions.share')}
                                    className="flex h-9 w-9 items-center justify-center rounded-lg border border-border text-muted-foreground transition-colors hover:border-primary hover:text-primary"
                                >
                                    <LinkIcon className="h-4 w-4" />
                                </button>

                                {cert.isReady && cert.downloadUrl ? (
                                    <a
                                        href={cert.downloadUrl}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 flex-1 sm:flex-initial"
                                    >
                                        <Download className="h-4 w-4" />
                                        {t('actions.download')}
                                    </a>
                                ) : (
                                    <span className="inline-flex flex-1 sm:flex-initial items-center justify-center gap-1 rounded-lg bg-muted px-4 py-2 text-sm text-muted-foreground">
                                        <Clock className="h-4 w-4" />
                                        {t('status.generating')}
                                    </span>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
