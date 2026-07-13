import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { KeyRound, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { queryKeys } from '@/api/queryKeys';
import { PasswordInput } from '@/components/common/form/PasswordInput';
import { TextButton } from '@/components/common/ui/TextButton';
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

interface ChangePasswordDialogProps {
    hasPassword?: boolean;
    email?: string;
}

export function ChangePasswordDialog({ hasPassword = true, email }: ChangePasswordDialogProps) {
    const { t } = useTranslation('profile');
    const queryClient = useQueryClient();
    const [isOpen, setIsOpen] = useState(false);

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
            // Setting a password flips `hasPassword` on the profile, which is what decides whether
            // this dialog offers "Set password" or "Change password". Without refetching it, the
            // cached profile still says the account has none and the button keeps its old label
            // until a reload.
            queryClient.invalidateQueries({ queryKey: queryKeys.users.myProfile() });
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
                    className="flex w-full items-center justify-center gap-2 py-2 text-sm font-medium text-muted-foreground transition-colors hover:text-primary sm:w-auto sm:justify-start sm:py-0"
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
                        <PasswordInput
                            label={t('changePasswordDialog.currentPassword')}
                            labelRightAction={
                                <TextButton
                                    onClick={handleForgotPassword}
                                    disabled={forgotPasswordMutation.isPending}
                                    className="text-sm"
                                >
                                    {forgotPasswordMutation.isPending && (
                                        <Loader2 className="mr-1 inline size-3 animate-spin" />
                                    )}
                                    {t('changePasswordDialog.forgotPassword')}
                                </TextButton>
                            }
                            error={
                                (form.formState.errors as Record<string, { message?: string }>)
                                    .currentPassword?.message
                            }
                            {...form.register(
                                'currentPassword' as
                                    | keyof SetPasswordFormData
                                    | keyof ChangePasswordFormData,
                            )}
                        />
                    )}

                    <PasswordInput
                        label={t('changePasswordDialog.newPassword')}
                        error={form.formState.errors.newPassword?.message}
                        {...form.register('newPassword')}
                    />

                    <PasswordInput
                        label={t('changePasswordDialog.confirmPassword')}
                        error={form.formState.errors.confirmPassword?.message}
                        {...form.register('confirmPassword')}
                    />

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
