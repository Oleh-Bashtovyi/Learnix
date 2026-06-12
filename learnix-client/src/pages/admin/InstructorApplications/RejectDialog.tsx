import { useState } from 'react';
import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface Props {
    applicantName: string;
    onConfirm: (reason: string | null) => void;
    onCancel: () => void;
    isLoading: boolean;
}

export function RejectDialog({ applicantName, onConfirm, onCancel, isLoading }: Props) {
    const { t } = useTranslation('admin');
    const [reason, setReason] = useState('');

    function handleConfirm() {
        onConfirm(reason.trim() || null);
    }

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <div className="w-full max-w-md rounded-xl border border-border bg-card shadow-lg">
                {/* Header */}
                <div className="flex items-center justify-between border-b border-border px-5 py-4">
                    <h2 className="font-heading font-semibold text-foreground">
                        {t('rejectDialogTitle')}
                    </h2>
                    <button
                        onClick={onCancel}
                        className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                    >
                        <X size={16} />
                    </button>
                </div>

                {/* Body */}
                <div className="space-y-4 px-5 py-4">
                    <p className="text-sm text-foreground">
                        {t('rejectDialogSubtitle', { name: applicantName })}
                    </p>
                    <div>
                        <label className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-muted-foreground">
                            {t('rejectReasonLabel')}
                        </label>
                        <textarea
                            value={reason}
                            onChange={(e) => setReason(e.target.value)}
                            placeholder={t('rejectReasonPlaceholder')}
                            rows={3}
                            className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                        />
                    </div>
                </div>

                {/* Footer */}
                <div className="flex justify-end gap-2 border-t border-border px-5 py-3">
                    <button
                        onClick={onCancel}
                        disabled={isLoading}
                        className="rounded-lg px-4 py-1.5 text-sm text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-50"
                    >
                        {t('rejectBtnCancel')}
                    </button>
                    <button
                        onClick={handleConfirm}
                        disabled={isLoading}
                        className="rounded-lg bg-destructive px-4 py-1.5 text-sm font-medium text-destructive-foreground transition-colors hover:bg-destructive/90 disabled:opacity-50"
                    >
                        {t('rejectBtnConfirm')}
                    </button>
                </div>
            </div>
        </div>
    );
}
