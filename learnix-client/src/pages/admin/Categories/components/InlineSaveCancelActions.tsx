import { useTranslation } from 'react-i18next';
import { Check, Loader2, X } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface InlineSaveCancelActionsProps {
    onSave: () => void;
    onCancel: () => void;
    isPending: boolean;
    saveDisabled?: boolean;
}

export function InlineSaveCancelActions({
    onSave,
    onCancel,
    isPending,
    saveDisabled = false,
}: InlineSaveCancelActionsProps) {
    const { t } = useTranslation('common');

    return (
        <>
            <Button
                variant="ghost"
                size="icon"
                onClick={onSave}
                disabled={saveDisabled || isPending}
                className="size-8 text-success hover:bg-success/10 hover:text-success disabled:opacity-40"
                title={t('actions.save')}
            >
                {isPending ? <Loader2 size={14} className="animate-spin" /> : <Check size={14} />}
            </Button>
            <Button
                variant="ghost"
                size="icon"
                onClick={onCancel}
                className="size-8 text-muted-foreground hover:bg-secondary"
                title={t('actions.cancel')}
            >
                <X size={14} />
            </Button>
        </>
    );
}
