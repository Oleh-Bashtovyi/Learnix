import { useState } from 'react';
import { LucideAlertTriangle } from 'lucide-react';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog';

interface ConfirmActionModalProps {
    title: string;
    description: string;
    trigger: React.ReactNode;
    onConfirm: () => void;
    confirmText?: string;
    cancelText?: string;
    variant?: 'danger' | 'warning' | 'info';
}

export const ConfirmActionModal = ({
    title,
    description,
    trigger,
    onConfirm,
    confirmText = 'Confirm',
    cancelText = 'Cancel',
    variant = 'danger',
}: ConfirmActionModalProps) => {
    const [open, setOpen] = useState(false);

    const handleConfirm = () => {
        onConfirm();
        setOpen(false);
    };

    const getButtonClass = () => {
        switch (variant) {
            case 'danger':
                return 'bg-red-600 text-white hover:bg-red-700';
            case 'warning':
                return 'bg-yellow-600 text-white hover:bg-yellow-700';
            case 'info':
                return 'bg-blue-600 text-white hover:bg-blue-700';
            default:
                return 'bg-indigo-600 text-white hover:bg-indigo-700';
        }
    };

    const getIconClass = () => {
        switch (variant) {
            case 'danger':
                return 'bg-red-100 text-red-600';
            case 'warning':
                return 'bg-yellow-100 text-yellow-600';
            case 'info':
                return 'bg-blue-100 text-blue-600';
            default:
                return 'bg-indigo-100 text-indigo-600';
        }
    };

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>{trigger}</DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader className="flex flex-col items-center gap-4 sm:flex-row sm:items-start">
                    <div className={`shrink-0 rounded-full p-3 ${getIconClass()}`}>
                        <LucideAlertTriangle className="size-6" />
                    </div>
                    <div className="flex flex-col space-y-1.5 text-center sm:text-left">
                        <DialogTitle className="text-xl font-semibold leading-none tracking-tight">
                            {title}
                        </DialogTitle>
                        <DialogDescription className="mt-2 text-sm text-slate-500">
                            {description}
                        </DialogDescription>
                    </div>
                </DialogHeader>
                <DialogFooter className="mt-6 flex gap-3 sm:justify-end sm:space-x-0">
                    <button
                        type="button"
                        onClick={() => setOpen(false)}
                        className="inline-flex h-10 items-center justify-center rounded-md border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-900 shadow-sm transition-colors hover:bg-slate-50 hover:text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-950 disabled:pointer-events-none disabled:opacity-50"
                    >
                        {cancelText}
                    </button>
                    <button
                        type="button"
                        onClick={handleConfirm}
                        className={`inline-flex h-10 items-center justify-center rounded-md px-4 py-2 text-sm font-semibold shadow transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-950 disabled:pointer-events-none disabled:opacity-50 ${getButtonClass()}`}
                    >
                        {confirmText}
                    </button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
};
