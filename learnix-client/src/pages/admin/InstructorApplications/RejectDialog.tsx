import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';

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
        <Dialog open onOpenChange={(open) => !open && !isLoading && onCancel()}>
            <DialogContent className="sm:max-w-md">
                <DialogHeader>
                    <DialogTitle>{t('rejectDialogTitle')}</DialogTitle>
                    <DialogDescription>
                        {t('rejectDialogSubtitle', { name: applicantName })}
                    </DialogDescription>
                </DialogHeader>

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

                <DialogFooter>
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
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
