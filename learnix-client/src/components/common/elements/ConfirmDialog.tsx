import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';

interface Props {
    title: string;
    description: string;
    confirmLabel: string;
    variant?: 'destructive' | 'warning' | 'default';
    isPending?: boolean;
    onConfirm: () => void;
    onClose: () => void;
}

export function ConfirmDialog({
    title,
    description,
    confirmLabel,
    variant = 'default',
    isPending = false,
    onConfirm,
    onClose,
}: Props) {
    const { t } = useTranslation('common');

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <div className="w-full max-w-sm rounded-xl border border-border bg-card shadow-lg">
                <div className="flex items-center justify-between border-b border-border px-5 py-4">
                    <h2 className="font-heading font-semibold text-foreground">{title}</h2>
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onClose}
                        disabled={isPending}
                        className="size-8 text-muted-foreground"
                    >
                        <X size={16} />
                    </Button>
                </div>

                <div className="px-5 py-4">
                    <p className="text-sm text-foreground">{description}</p>
                </div>

                <div className="flex justify-end gap-2 border-t border-border px-5 py-3">
                    <Button variant="ghost" onClick={onClose} disabled={isPending}>
                        {t('actions.cancel')}
                    </Button>
                    <AsyncButton variant={variant} onClick={onConfirm} isLoading={isPending}>
                        {confirmLabel}
                    </AsyncButton>
                </div>
            </div>
        </div>
    );
}
