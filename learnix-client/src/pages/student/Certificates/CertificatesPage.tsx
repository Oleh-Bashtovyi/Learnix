import { useTranslation } from 'react-i18next';
import { Download, GraduationCap, Link as LinkIcon, RefreshCw } from 'lucide-react';
import { toast } from 'sonner';
import { useGenerateCertificate } from '@/hooks/user/useGenerateCertificate';
import { useMyCertificates } from '@/hooks/user/useMyCertificates';
import type { MyCertificateDto } from '@/types/certificate.types';
import { cn } from '@/utils/cn';

type CertificateCardProps = {
    cert: MyCertificateDto;
};

function CertificateCard({ cert }: CertificateCardProps) {
    const { t } = useTranslation('certificates');
    const generateMutation = useGenerateCertificate();

    function copyLink(url: string) {
        navigator.clipboard.writeText(url);
        toast.success(t('messages.linkCopied'));
    }

    const handleGenerate = async () => {
        try {
            const url = await generateMutation.mutateAsync(cert.courseId);
            window.location.href = url;
        } catch (error) {
            console.error('Failed to generate certificate:', error);
        }
    };

    return (
        <div className="flex flex-col gap-4 rounded-xl border border-border bg-card p-4 transition-shadow hover:shadow-sm sm:flex-row sm:items-center sm:gap-5 sm:p-5">
            {/* Info */}
            <div className="flex min-w-0 flex-1 items-center gap-3 sm:gap-5">
                <div
                    className={cn(
                        'flex h-12 w-12 shrink-0 items-center justify-center rounded-lg sm:h-16 sm:w-16',
                        cert.courseCoverBlobPath ? 'overflow-hidden' : 'bg-accent/10',
                    )}
                >
                    {cert.courseCoverBlobPath ? (
                        <img
                            src={cert.courseCoverBlobPath}
                            alt={cert.courseTitle}
                            className="size-full object-cover"
                        />
                    ) : (
                        <GraduationCap className="size-6 text-accent sm:size-8" />
                    )}
                </div>
                <div className="min-w-0 flex-1">
                    <p className="line-clamp-2 font-heading text-sm font-semibold text-foreground sm:line-clamp-1 sm:text-base">
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
                </div>
            </div>

            {/* Actions */}
            <div className="flex w-full shrink-0 items-center justify-end gap-2 sm:w-auto sm:justify-start">
                <button
                    type="button"
                    onClick={() => copyLink(cert.verificationUrl)}
                    title={t('actions.share')}
                    className="flex size-9 items-center justify-center rounded-lg border border-border text-muted-foreground transition-colors hover:border-primary hover:text-primary"
                >
                    <LinkIcon className="size-4" />
                </button>

                {cert.downloadUrl ? (
                    <>
                        <a
                            href={cert.downloadUrl}
                            className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90"
                        >
                            <Download className="size-4" />
                            {t('actions.download', { defaultValue: 'Download' })}
                        </a>
                        <button
                            type="button"
                            onClick={handleGenerate}
                            disabled={generateMutation.isPending}
                            title="Regenerate Certificate"
                            className="inline-flex items-center justify-center gap-2 rounded-lg border border-border bg-transparent px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                        >
                            <RefreshCw
                                className={cn(
                                    'h-4 w-4',
                                    generateMutation.isPending && 'animate-spin',
                                )}
                            />
                            <span className="hidden sm:inline">Regenerate</span>
                        </button>
                    </>
                ) : (
                    <button
                        type="button"
                        onClick={handleGenerate}
                        disabled={generateMutation.isPending}
                        className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 sm:flex-initial"
                    >
                        {generateMutation.isPending ? (
                            <RefreshCw className="size-4 animate-spin" />
                        ) : (
                            <Download className="size-4" />
                        )}
                        {generateMutation.isPending
                            ? t('status.generating', { defaultValue: 'Generating...' })
                            : 'Generate PDF'}
                    </button>
                )}
            </div>
        </div>
    );
}

export default function CertificatesPage() {
    const { t } = useTranslation('certificates');
    const { data: certificates, isLoading } = useMyCertificates();

    if (isLoading) {
        return (
            <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
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
        <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
            {!certificates || certificates.length === 0 ? (
                <div className="mt-16 text-center">
                    <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-accent/10">
                        <GraduationCap className="size-12 text-accent" />
                    </div>
                    <h2 className="mt-6 font-heading text-2xl font-bold">{t('emptyTitle')}</h2>
                    <p className="mt-2 text-muted-foreground">{t('emptyDescription')}</p>
                </div>
            ) : (
                <div className="max-w-4xl space-y-4">
                    {certificates.map((cert) => (
                        <CertificateCard key={cert.certificateId} cert={cert} />
                    ))}
                </div>
            )}
        </div>
    );
}
