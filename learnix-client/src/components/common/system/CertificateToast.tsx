import { useTranslation } from 'react-i18next';
import { Award, X } from 'lucide-react';

interface CertificateToastProps {
    courseTitle: string;
    onViewAll: () => void;
    onDismiss: () => void;
}

export function CertificateToast({ courseTitle, onViewAll, onDismiss }: CertificateToastProps) {
    const { t } = useTranslation('certificates');

    return (
        <div className="flex w-full items-start gap-3 rounded-lg border border-brand/40 bg-brand/10 p-4 shadow-lg">
            <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-brand/15">
                <Award className="size-5 text-brand" />
            </div>

            <div className="min-w-0 flex-1">
                <p className="font-heading text-sm font-semibold text-card-foreground">
                    {t('notification.title')}
                </p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                    {t('notification.descriptionPrefix')}"{courseTitle}"
                </p>
                <button
                    type="button"
                    onClick={onViewAll}
                    className="mt-2 text-xs font-medium text-brand hover:underline"
                >
                    {t('notification.action')}
                </button>
            </div>

            <button
                type="button"
                onClick={onDismiss}
                aria-label={t('common:actions.close', { defaultValue: 'Close' })}
                className="shrink-0 rounded p-0.5 text-muted-foreground transition-colors hover:text-foreground"
            >
                <X className="size-4" />
            </button>
        </div>
    );
}
