import { X } from 'lucide-react';
import { cn } from '@/utils/cn';

interface Props {
    title: string;
    description: string;
    confirmLabel: string;
    variant?: 'destructive' | 'warning' | 'default';
    isPending?: boolean;
    onConfirm: () => void;
    onClose: () => void;
}

const CONFIRM_STYLES = {
    destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/90',
    warning: 'bg-warning text-warning-foreground hover:bg-warning/90',
    default: 'bg-primary text-primary-foreground hover:bg-primary/90',
};

export function ConfirmDialog({
    title,
    description,
    confirmLabel,
    variant = 'default',
    isPending = false,
    onConfirm,
    onClose,
}: Props) {
    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <div className="w-full max-w-sm rounded-xl border border-border bg-card shadow-lg">
                <div className="flex items-center justify-between border-b border-border px-5 py-4">
                    <h2 className="font-heading font-semibold text-foreground">{title}</h2>
                    <button
                        onClick={onClose}
                        disabled={isPending}
                        className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-50"
                    >
                        <X size={16} />
                    </button>
                </div>

                <div className="px-5 py-4">
                    <p className="text-sm text-foreground">{description}</p>
                </div>

                <div className="flex justify-end gap-2 border-t border-border px-5 py-3">
                    <button
                        onClick={onClose}
                        disabled={isPending}
                        className="rounded-lg px-4 py-1.5 text-sm text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-50"
                    >
                        Cancel
                    </button>
                    <button
                        onClick={onConfirm}
                        disabled={isPending}
                        className={cn(
                            'rounded-lg px-4 py-1.5 text-sm font-medium transition-colors disabled:opacity-50',
                            CONFIRM_STYLES[variant],
                        )}
                    >
                        {confirmLabel}
                    </button>
                </div>
            </div>
        </div>
    );
}
