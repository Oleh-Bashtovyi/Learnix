import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { Eye, EyeOff, KeyRound, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog';
import { APP_ROUTES } from '@/routes/paths';
import { type ChangePasswordFormData, changePasswordSchema } from '@/schemas/auth.schema';
import { cn } from '@/utils/cn';

export function ChangePasswordDialog() {
    const { t } = useTranslation('profile');
    const [isOpen, setIsOpen] = useState(false);
    const [showCurrentPassword, setShowCurrentPassword] = useState(false);
    const [showNewPassword, setShowNewPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    const form = useForm<ChangePasswordFormData>({
        resolver: zodResolver(changePasswordSchema),
        defaultValues: {
            currentPassword: '',
            newPassword: '',
            confirmPassword: '',
        },
    });

    const mutation = useMutation({
        mutationFn: authApi.changePassword,
        onSuccess: () => {
            toast.success(t('changePasswordDialog.success'));
            setIsOpen(false);
            form.reset();
        },
        onError: (error: unknown) => {
            const err = error as { response?: { data?: { title?: string } }; message?: string };
            const message = err.response?.data?.title || err.message || 'An error occurred';
            toast.error(message);
        },
    });

    const onSubmit = (data: ChangePasswordFormData) => {
        mutation.mutate({
            currentPassword: data.currentPassword,
            newPassword: data.newPassword,
        });
    };

    return (
        <Dialog
            open={isOpen}
            onOpenChange={(open) => {
                setIsOpen(open);
                if (!open) form.reset();
            }}
        >
            <DialogTrigger asChild>
                <button
                    type="button"
                    className="mt-1 flex items-center gap-2 transition-colors hover:text-primary"
                >
                    <KeyRound className="size-4 shrink-0" />
                    <span>{t('avatar.changePassword')}</span>
                </button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-md">
                <DialogHeader>
                    <DialogTitle>{t('changePasswordDialog.title')}</DialogTitle>
                    <DialogDescription>{t('changePasswordDialog.description')}</DialogDescription>
                </DialogHeader>

                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                    <div className="space-y-1.5">
                        <div className="flex items-center justify-between">
                            <label className="text-sm font-medium text-foreground">
                                {t('changePasswordDialog.currentPassword')}
                            </label>
                            <Link
                                to={APP_ROUTES.public.forgotPassword}
                                className="text-xs text-primary hover:underline"
                                onClick={() => setIsOpen(false)}
                            >
                                {t('changePasswordDialog.forgotPassword')}
                            </Link>
                        </div>
                        <div className="relative">
                            <input
                                type={showCurrentPassword ? 'text' : 'password'}
                                className={cn(
                                    'w-full rounded-lg border bg-background px-3 py-2 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2 focus:ring-ring',
                                    form.formState.errors.currentPassword
                                        ? 'border-destructive focus:ring-destructive/10'
                                        : 'border-input',
                                )}
                                {...form.register('currentPassword')}
                            />
                            <button
                                type="button"
                                onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                                tabIndex={-1}
                            >
                                {showCurrentPassword ? (
                                    <EyeOff className="size-4" />
                                ) : (
                                    <Eye className="size-4" />
                                )}
                            </button>
                        </div>
                        {form.formState.errors.currentPassword && (
                            <p className="text-sm text-destructive">
                                {form.formState.errors.currentPassword.message}
                            </p>
                        )}
                    </div>

                    <div className="space-y-1.5">
                        <label className="text-sm font-medium text-foreground">
                            {t('changePasswordDialog.newPassword')}
                        </label>
                        <div className="relative">
                            <input
                                type={showNewPassword ? 'text' : 'password'}
                                className={cn(
                                    'w-full rounded-lg border bg-background px-3 py-2 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2 focus:ring-ring',
                                    form.formState.errors.newPassword
                                        ? 'border-destructive focus:ring-destructive/10'
                                        : 'border-input',
                                )}
                                {...form.register('newPassword')}
                            />
                            <button
                                type="button"
                                onClick={() => setShowNewPassword(!showNewPassword)}
                                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                                tabIndex={-1}
                            >
                                {showNewPassword ? (
                                    <EyeOff className="size-4" />
                                ) : (
                                    <Eye className="size-4" />
                                )}
                            </button>
                        </div>
                        {form.formState.errors.newPassword && (
                            <p className="text-sm text-destructive">
                                {form.formState.errors.newPassword.message}
                            </p>
                        )}
                    </div>

                    <div className="space-y-1.5">
                        <label className="text-sm font-medium text-foreground">
                            {t('changePasswordDialog.confirmPassword')}
                        </label>
                        <div className="relative">
                            <input
                                type={showConfirmPassword ? 'text' : 'password'}
                                className={cn(
                                    'w-full rounded-lg border bg-background px-3 py-2 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2 focus:ring-ring',
                                    form.formState.errors.confirmPassword
                                        ? 'border-destructive focus:ring-destructive/10'
                                        : 'border-input',
                                )}
                                {...form.register('confirmPassword')}
                            />
                            <button
                                type="button"
                                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                                tabIndex={-1}
                            >
                                {showConfirmPassword ? (
                                    <EyeOff className="size-4" />
                                ) : (
                                    <Eye className="size-4" />
                                )}
                            </button>
                        </div>
                        {form.formState.errors.confirmPassword && (
                            <p className="text-sm text-destructive">
                                {form.formState.errors.confirmPassword.message}
                            </p>
                        )}
                    </div>

                    <div className="flex justify-end pt-4">
                        <Button type="submit" disabled={mutation.isPending}>
                            {mutation.isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
                            {t('changePasswordDialog.submit')}
                        </Button>
                    </div>
                </form>
            </DialogContent>
        </Dialog>
    );
}
