import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
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
import {
    type ChangePasswordFormData,
    type SetPasswordFormData,
    changePasswordSchema,
    setPasswordSchema,
} from '@/schemas/auth.schema';
import { cn } from '@/utils/cn';

interface ChangePasswordDialogProps {
    hasPassword?: boolean;
    email?: string;
}

export function ChangePasswordDialog({ hasPassword = true, email }: ChangePasswordDialogProps) {
    const { t } = useTranslation('profile');
    const [isOpen, setIsOpen] = useState(false);
    const [showCurrentPassword, setShowCurrentPassword] = useState(false);
    const [showNewPassword, setShowNewPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    // We use a dynamic form based on hasPassword
    const form = useForm<ChangePasswordFormData | SetPasswordFormData>({
        resolver: zodResolver(hasPassword ? changePasswordSchema : setPasswordSchema),
        defaultValues: {
            currentPassword: '',
            newPassword: '',
            confirmPassword: '',
        },
    });

    const changePasswordMutation = useMutation({
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

    const setPasswordMutation = useMutation({
        mutationFn: authApi.setPassword,
        onSuccess: () => {
            toast.success(t('setPasswordDialog.success'));
            setIsOpen(false);
            form.reset();
        },
        onError: (error: unknown) => {
            const err = error as { response?: { data?: { title?: string } }; message?: string };
            const message = err.response?.data?.title || err.message || 'An error occurred';
            toast.error(message);
        },
    });

    const forgotPasswordMutation = useMutation({
        mutationFn: authApi.forgotPassword,
        onSuccess: () => {
            toast.success(t('changePasswordDialog.resetLinkSent'));
            setIsOpen(false);
        },
        onError: (error: unknown) => {
            const err = error as { response?: { data?: { title?: string } }; message?: string };
            const message = err.response?.data?.title || err.message || 'An error occurred';
            toast.error(message);
        },
    });

    const onSubmit = (data: ChangePasswordFormData | SetPasswordFormData) => {
        if (hasPassword) {
            changePasswordMutation.mutate({
                currentPassword: (data as ChangePasswordFormData).currentPassword,
                newPassword: data.newPassword,
            });
        } else {
            setPasswordMutation.mutate({
                newPassword: data.newPassword,
            });
        }
    };

    const handleForgotPassword = () => {
        if (email) {
            forgotPasswordMutation.mutate({ email });
        }
    };

    const isPending = changePasswordMutation.isPending || setPasswordMutation.isPending;
    const dialogKey = hasPassword ? 'changePasswordDialog' : 'setPasswordDialog';

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
                    <span>{t(`avatar.${hasPassword ? 'changePassword' : 'setPassword'}`)}</span>
                </button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-md">
                <DialogHeader>
                    <DialogTitle>{t(`${dialogKey}.title`)}</DialogTitle>
                    <DialogDescription>{t(`${dialogKey}.description`)}</DialogDescription>
                </DialogHeader>

                <form
                    onSubmit={(e) => {
                        e.stopPropagation();
                        form.handleSubmit(onSubmit)(e);
                    }}
                    className="space-y-4"
                >
                    {hasPassword && (
                        <div className="space-y-1.5">
                            <div className="flex items-center justify-between">
                                <label className="text-sm font-medium text-foreground">
                                    {t('changePasswordDialog.currentPassword')}
                                </label>
                                <button
                                    type="button"
                                    onClick={handleForgotPassword}
                                    disabled={forgotPasswordMutation.isPending}
                                    className="text-xs text-primary hover:underline disabled:opacity-50"
                                >
                                    {forgotPasswordMutation.isPending ? (
                                        <Loader2 className="mr-1 inline size-3 animate-spin" />
                                    ) : null}
                                    {t('changePasswordDialog.forgotPassword')}
                                </button>
                            </div>
                            <div className="relative">
                                <input
                                    type={showCurrentPassword ? 'text' : 'password'}
                                    className={cn(
                                        'w-full rounded-lg border bg-background px-3 py-2 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2 focus:ring-ring [&::-ms-reveal]:hidden',
                                        (
                                            form.formState.errors as Record<
                                                string,
                                                { message?: string }
                                            >
                                        ).currentPassword
                                            ? 'border-destructive focus:ring-destructive/10'
                                            : 'border-input',
                                    )}
                                    {...form.register(
                                        'currentPassword' as
                                            | keyof SetPasswordFormData
                                            | keyof ChangePasswordFormData,
                                    )}
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
                            {(form.formState.errors as Record<string, { message?: string }>)
                                .currentPassword && (
                                <p className="text-sm text-destructive">
                                    {
                                        (
                                            form.formState.errors as Record<
                                                string,
                                                { message?: string }
                                            >
                                        ).currentPassword!.message
                                    }
                                </p>
                            )}
                        </div>
                    )}

                    <div className="space-y-1.5">
                        <label className="text-sm font-medium text-foreground">
                            {t('changePasswordDialog.newPassword')}
                        </label>
                        <div className="relative">
                            <input
                                type={showNewPassword ? 'text' : 'password'}
                                className={cn(
                                    'w-full rounded-lg border bg-background px-3 py-2 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2 focus:ring-ring [&::-ms-reveal]:hidden',
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
                                    'w-full rounded-lg border bg-background px-3 py-2 pr-10 text-sm text-foreground outline-none transition-all placeholder:text-muted-foreground focus:ring-2 focus:ring-ring [&::-ms-reveal]:hidden',
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
                        <Button type="submit" disabled={isPending}>
                            {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
                            {t(`${dialogKey}.submit`)}
                        </Button>
                    </div>
                </form>
            </DialogContent>
        </Dialog>
    );
}
