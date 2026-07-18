import { useTranslation } from 'react-i18next';
import { Award, X } from 'lucide-react';

interface CertificateToastProps {
    courseTitle: string;
    onViewAll: () => void;
    onRate: () => void;
    onDismiss: () => void;
}

export function CertificateToast({
    courseTitle,
    onViewAll,
    onRate,
    onDismiss,
}: CertificateToastProps) {
    const { t } = useTranslation('certificates');

    return (
        <div className="relative flex w-full items-start gap-3 overflow-hidden rounded-lg border border-brand/40 bg-background p-4 shadow-lg">
            <div className="pointer-events-none absolute inset-0 bg-brand/10" />

            <div className="relative z-10 flex size-9 shrink-0 items-center justify-center rounded-full bg-brand/15">
                <Award className="size-5 text-brand" />
            </div>

            <div className="relative z-10 min-w-0 flex-1">
                <p className="font-heading text-sm font-semibold text-card-foreground">
                    {t('notification.title')}
                </p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                    {t('notification.descriptionPrefix')}"{courseTitle}"
                </p>
                <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1">
                    <button
                        type="button"
                        onClick={onViewAll}
                        className="text-xs font-medium text-brand hover:underline"
                    >
                        {t('notification.action')}
                    </button>
                    <button
                        type="button"
                        onClick={onRate}
                        className="text-xs font-medium text-brand hover:underline"
                    >
                        {t('notification.rateAction')}
                    </button>
                </div>
            </div>

            <button
                type="button"
                onClick={onDismiss}
                aria-label={t('common:actions.close', { defaultValue: 'Close' })}
                className="relative z-10 shrink-0 rounded p-0.5 text-muted-foreground transition-colors hover:text-foreground"
            >
                <X className="size-4" />
            </button>
        </div>
    );
}
