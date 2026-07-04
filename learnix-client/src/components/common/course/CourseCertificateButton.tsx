import { useTranslation } from 'react-i18next';
import { Award, RefreshCw } from 'lucide-react';
import { useGenerateCertificate } from '@/hooks/user/useGenerateCertificate';
import { useMyCertificates } from '@/hooks/user/useMyCertificates';
import { cn } from '@/utils/cn';

interface CourseCertificateButtonProps {
    courseId: string;
    variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
    className?: string;
    showIconOnlyOnMobile?: boolean;
}

export function CourseCertificateButton({
    courseId,
    variant = 'primary',
    className,
    showIconOnlyOnMobile = false,
}: CourseCertificateButtonProps) {
    const { t } = useTranslation('certificates');
    const { data: certificates, isLoading } = useMyCertificates();
    const generateMutation = useGenerateCertificate();

    const certificate = certificates?.find((c) => c.courseId === courseId);

    if (isLoading) {
        return (
            <div
                className={cn(
                    'inline-flex animate-pulse items-center justify-center rounded-lg bg-muted px-4 py-2',
                    className,
                )}
            >
                <div className="h-4 w-24 rounded bg-muted-foreground/20" />
            </div>
        );
    }

    if (!certificate) {
        return null;
    }

    const baseStyles =
        'inline-flex items-center justify-center gap-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-50 disabled:pointer-events-none';

    const variants = {
        primary: 'bg-primary text-primary-foreground hover:bg-primary/90',
        secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
        outline: 'border border-border bg-transparent hover:bg-secondary',
        ghost: 'hover:bg-accent hover:text-accent-foreground text-muted-foreground',
    };

    const handleGenerate = async () => {
        try {
            const url = await generateMutation.mutateAsync(courseId);
            window.open(url, '_blank', 'noopener,noreferrer');
        } catch (error) {
            console.error('Failed to generate certificate:', error);
        }
    };

    if (certificate.downloadUrl) {
        return (
            <div className="flex items-center gap-2">
                <a
                    href={certificate.downloadUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className={cn(baseStyles, variants[variant], 'px-4 py-2', className)}
                >
                    <Award className="size-4 shrink-0" />
                    <span
                        className={cn(
                            'whitespace-nowrap',
                            showIconOnlyOnMobile && 'hidden sm:inline',
                        )}
                    >
                        {t('actions.download', { defaultValue: 'Download Certificate' })}
                    </span>
                </a>
                <button
                    type="button"
                    onClick={handleGenerate}
                    disabled={generateMutation.isPending}
                    title="Regenerate Certificate"
                    className={cn(baseStyles, variants['outline'], 'px-3 py-2', className)}
                >
                    <RefreshCw
                        className={cn('h-4 w-4', generateMutation.isPending && 'animate-spin')}
                    />
                </button>
            </div>
        );
    }

    return (
        <button
            type="button"
            onClick={handleGenerate}
            disabled={generateMutation.isPending}
            className={cn(baseStyles, variants[variant], 'px-4 py-2', className)}
        >
            {generateMutation.isPending ? (
                <RefreshCw className="size-4 shrink-0 animate-spin" />
            ) : (
                <Award className="size-4 shrink-0" />
            )}
            <span className={cn('whitespace-nowrap', showIconOnlyOnMobile && 'hidden sm:inline')}>
                {generateMutation.isPending
                    ? t('status.generating', { defaultValue: 'Generating...' })
                    : 'Generate PDF'}
            </span>
        </button>
    );
}
