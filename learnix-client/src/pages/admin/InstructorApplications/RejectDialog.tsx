import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';

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
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onCancel}
                        className="size-8 text-muted-foreground"
                    >
                        <X size={16} />
                    </Button>
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
                        <FormTextarea
                            variant="card"
                            value={reason}
                            onChange={(e) => setReason(e.target.value)}
                            placeholder={t('rejectReasonPlaceholder')}
                            rows={3}
                        />
                    </div>
                </div>

                {/* Footer */}
                <div className="flex justify-end gap-2 border-t border-border px-5 py-3">
                    <Button variant="ghost" onClick={onCancel} disabled={isLoading}>
                        {t('common:actions.cancel')}
                    </Button>
                    <AsyncButton
                        variant="destructive"
                        onClick={handleConfirm}
                        isLoading={isLoading}
                        loadingText={t('common:actions.submitting')}
                    >
                        {t('rejectBtnConfirm')}
                    </AsyncButton>
                </div>
            </div>
        </div>
    );
}
