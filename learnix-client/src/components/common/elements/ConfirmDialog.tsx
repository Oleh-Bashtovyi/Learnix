import { useTranslation } from 'react-i18next';
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
        <Dialog open onOpenChange={(open) => !open && !isPending && onClose()}>
            <DialogContent className="sm:max-w-sm">
                <DialogHeader>
                    <DialogTitle>{title}</DialogTitle>
                    <DialogDescription>{description}</DialogDescription>
                </DialogHeader>
                <DialogFooter>
                    <Button variant="ghost" onClick={onClose} disabled={isPending}>
                        {t('actions.cancel')}
                    </Button>
                    <AsyncButton
                        variant={variant}
                        onClick={onConfirm}
                        isLoading={isPending}
                        loadingText={confirmLabel}
                    >
                        {confirmLabel}
                    </AsyncButton>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
