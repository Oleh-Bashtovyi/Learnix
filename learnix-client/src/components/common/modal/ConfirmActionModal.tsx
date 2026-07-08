import { useState } from 'react';
import { LucideAlertTriangle } from 'lucide-react';
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';
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
    onConfirm: () => void | Promise<void>;
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
    const [isLoading, setIsLoading] = useState(false);
    const [isSuccess, setIsSuccess] = useState(false);

    const handleConfirm = async () => {
        setIsLoading(true);
        try {
            await onConfirm();
            setIsSuccess(true);
            setTimeout(() => {
                setOpen(false);
            }, 1000);
        } catch (error) {
            console.error(error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleOpenChange = (newOpen: boolean) => {
        if (!newOpen) {
            setTimeout(() => setIsSuccess(false), 300);
        }
        setOpen(newOpen);
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
        <Dialog open={open} onOpenChange={handleOpenChange}>
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
                    <Button
                        type="button"
                        variant="outline"
                        onClick={() => setOpen(false)}
                        disabled={isLoading || isSuccess}
                    >
                        {cancelText}
                    </Button>
                    <AsyncButton
                        type="button"
                        onClick={handleConfirm}
                        className={getButtonClass()}
                        isLoading={isLoading}
                        isSuccess={isSuccess}
                        loadingText="Submitting..."
                    >
                        {confirmText}
                    </AsyncButton>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
};
