import type { UseFormReturn } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, CheckCircle2, Mail } from 'lucide-react';
import { FormInput } from '@/components/common/form/FormInput';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import type { ProfileFormValues } from '@/schemas/profile.schema';
import { cn } from '@/utils/cn';

interface ProfileFormSectionProps {
    form: UseFormReturn<ProfileFormValues>;
    user: { emailVerified: boolean; email: string } | null;
    resendCooldown: number;
    isResending: boolean;
    onResendEmail: () => void;
}

export function ProfileFormSection({
    form,
    user,
    resendCooldown,
    isResending,
    onResendEmail,
}: ProfileFormSectionProps) {
    const { t } = useTranslation('profile');
    const { t: tEmail } = useTranslation('emailConfirmation');

    return (
        <div className="flex-1 space-y-5 sm:space-y-6">
            <div className="grid gap-4 sm:grid-cols-2">
                <FormInput
                    label={t('common:general.firstName')}
                    error={form.formState.errors.firstName?.message}
                    variant="muted"
                    {...form.register('firstName')}
                />

                <FormInput
                    label={t('common:general.lastName')}
                    error={form.formState.errors.lastName?.message}
                    variant="muted"
                    {...form.register('lastName')}
                />
            </div>

            <div>
                <label className="block text-sm font-medium text-foreground">
                    {t('common:general.email')}
                </label>
                <div className="relative mt-1">
                    <FormInput
                        value={user?.email ?? ''}
                        readOnly
                        variant="muted"
                        className={cn(
                            'cursor-not-allowed pl-9 pr-9',
                            user?.emailVerified ? 'border-success/50' : 'border-border',
                        )}
                    />
                    <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                        <Mail className="size-4 text-muted-foreground" />
                    </div>
                    {user?.emailVerified && (
                        <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3">
                            <div className="flex size-5 items-center justify-center rounded-full bg-success/20">
                                <CheckCircle2 className="size-3.5 text-success" />
                            </div>
                        </div>
                    )}
                </div>
                {user && (
                    <p
                        className={cn(
                            'mt-2 flex items-center gap-1.5 text-xs font-medium',
                            user.emailVerified
                                ? 'text-success'
                                : 'text-amber-600 dark:text-amber-400',
                        )}
                    >
                        {user.emailVerified ? (
                            <>
                                <CheckCircle2 className="size-3.5" />
                                {tEmail('profile.confirmed')}
                            </>
                        ) : (
                            <>
                                <AlertTriangle className="size-3.5" />
                                {tEmail('profile.notConfirmed')}
                            </>
                        )}
                    </p>
                )}
            </div>

            {user && !user.emailVerified && (
                <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/50 dark:bg-amber-950/30">
                    <p className="text-sm text-amber-800 dark:text-amber-300">
                        {tEmail('profile.notConfirmedHint')}
                    </p>
                    <button
                        type="button"
                        onClick={onResendEmail}
                        disabled={resendCooldown > 0 || isResending}
                        className="mt-3 rounded-lg border border-amber-300 bg-white px-4 py-2 text-sm font-medium text-amber-800 shadow-sm transition-colors hover:bg-amber-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-amber-800 dark:bg-transparent dark:text-amber-300"
                    >
                        {resendCooldown > 0
                            ? tEmail('profile.resendCooldown', {
                                  seconds: resendCooldown,
                              })
                            : tEmail('profile.resend')}
                    </button>
                </div>
            )}

            <FormTextarea
                label={t('fields.bio')}
                rows={4}
                placeholder={t('fields.bioPlaceholder')}
                error={form.formState.errors.bio?.message}
                variant="muted"
                {...form.register('bio')}
            />
        </div>
    );
}
