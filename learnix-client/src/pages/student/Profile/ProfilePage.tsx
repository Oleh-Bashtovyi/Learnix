import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { authApi } from '@/api/auth.api';
import { AUTH_LIMITS } from '@/const/auth.constants';
import { useRequestUploadUrl } from '@/hooks/shared/useRequestUploadUrl';
import { useMyAchievements } from '@/hooks/user/useMyAchievements';
import { useMyProfile } from '@/hooks/user/useMyProfile';
import { useUpdateProfile } from '@/hooks/user/useUpdateProfile';
import { APP_ROUTES } from '@/routes/paths';
import { type ProfileFormValues, profileSchema } from '@/schemas/profile.schema';
import { useAuthStore } from '@/store/auth.store';
import { isValidationError } from '@/utils/errors';
import { AchievementsSection } from './components/AchievementsSection';
import { AvatarUpload } from './components/AvatarUpload';
import { ChangePasswordDialog } from './components/ChangePasswordDialog';
import { ProfileFormSection } from './components/ProfileFormSection';
import { QuickNavSection } from './components/QuickNavSection';

export default function ProfilePage() {
    const { t } = useTranslation('profile');
    const { data: profile, isLoading } = useMyProfile();
    const updateProfile = useUpdateProfile();
    const { uploadFile, isUploading } = useRequestUploadUrl();
    const { data: achievementsData, isLoading: achievementsLoading } = useMyAchievements();
    const user = useAuthStore((s) => s.user);
    const navigate = useNavigate();
    const location = useLocation();
    const [resendCooldown, setResendCooldown] = useState(0);

    const unlockedMap = new Map(achievementsData?.unlocked.map((a) => [a.code, a]));

    useEffect(() => {
        if (resendCooldown <= 0) return;
        const timer = setTimeout(() => setResendCooldown((c) => c - 1), 1000);
        return () => clearTimeout(timer);
    }, [resendCooldown]);

    const { mutate: resendEmail, isPending: isResending } = useMutation({
        mutationFn: () => authApi.resendConfirmation({ email: user!.email }),
        onSuccess: () => {
            setResendCooldown(AUTH_LIMITS.RESEND_COOLDOWN_SECONDS);
            toast.success('Confirmation email sent. Check your inbox.');
            navigate(APP_ROUTES.public.verifyEmail, {
                state: { email: user!.email, from: location.pathname },
            });
        },
        meta: { suppressGlobalError: true },
        onError: () => toast.error('Failed to resend. Please try again later.'),
    });

    const [avatarBlobPath, setAvatarBlobPath] = useState<string | null>(null);
    const [avatarPreview, setAvatarPreview] = useState<string | null>(null);

    const form = useForm<ProfileFormValues>({
        resolver: zodResolver(profileSchema),
        defaultValues: { firstName: '', lastName: '', bio: '' },
    });

    useEffect(() => {
        if (profile) {
            form.reset({
                firstName: profile.firstName,
                lastName: profile.lastName,
                bio: profile.bio ?? '',
            });
        }
    }, [profile, form]);

    async function handleAvatarChange(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0];
        if (!file) return;

        setAvatarPreview(URL.createObjectURL(file));
        try {
            const blobPath = await uploadFile('Avatar', file);
            setAvatarBlobPath(blobPath);
        } catch {
            setAvatarPreview(null);
            toast.error('Avatar upload failed. Please try again.');
        }
    }

    async function onSubmit(values: ProfileFormValues) {
        try {
            await updateProfile.mutateAsync({
                firstName: values.firstName,
                lastName: values.lastName,
                bio: values.bio || null,
                avatarBlobPath,
            });
            toast.success(t('messages.saveSuccess'));
        } catch (error) {
            if (isValidationError(error)) {
                const errors = error.response!.data.errors!;
                Object.entries(errors).forEach(([field, messages]) => {
                    form.setError(
                        (field.charAt(0).toLowerCase() + field.slice(1)) as keyof ProfileFormValues,
                        {
                            message: messages.join('. '),
                        },
                    );
                });
            }
        }
    }

    if (isLoading) {
        return (
            <div className="mx-auto max-w-3xl px-4 py-12 sm:px-6 md:py-20">
                <div className="animate-pulse space-y-6">
                    <div className="h-8 w-48 rounded bg-muted" />
                    <div className="h-32 rounded-xl bg-muted" />
                    <div className="h-48 rounded-xl bg-muted" />
                </div>
            </div>
        );
    }

    const displayAvatar = avatarPreview ?? profile?.avatarUrl ?? null;

    return (
        <div className="mx-auto max-w-3xl p-4 sm:p-6">
            <h1 className="font-heading text-2xl font-bold text-foreground sm:text-3xl">
                {t('pageTitle')}
            </h1>

            <div className="mt-4 space-y-6 sm:mt-6">
                {/* Profile Information */}
                <form onSubmit={form.handleSubmit(onSubmit)}>
                    <section className="rounded-xl border border-border bg-card">
                        <div className="border-b border-border p-4 sm:px-6">
                            <h2 className="font-heading text-lg font-semibold">
                                {t('sections.personal')}
                            </h2>
                        </div>

                        <div className="flex flex-col gap-5 p-4 sm:gap-6 sm:p-6 md:flex-row md:items-start md:gap-8">
                            {/* Left column: Avatar */}
                            <AvatarUpload
                                firstName={profile?.firstName}
                                lastName={profile?.lastName}
                                displayAvatar={displayAvatar}
                                isUploading={isUploading}
                                onAvatarChange={handleAvatarChange}
                                createdAt={profile?.createdAt}
                            />

                            {/* Right column: Form Fields */}
                            <ProfileFormSection
                                form={form}
                                user={user}
                                resendCooldown={resendCooldown}
                                isResending={isResending}
                                onResendEmail={() => resendEmail()}
                            />
                        </div>

                        <div className="flex flex-col-reverse justify-between gap-3 border-t border-border bg-muted/20 p-3 sm:flex-row sm:items-center sm:px-5">
                            <div className="w-full sm:w-auto">
                                <ChangePasswordDialog
                                    hasPassword={profile?.hasPassword}
                                    email={profile?.email}
                                />
                            </div>
                            <button
                                type="submit"
                                disabled={
                                    updateProfile.isPending ||
                                    isUploading ||
                                    (!form.formState.isDirty && avatarBlobPath === null)
                                }
                                className="w-full rounded-lg bg-primary px-10 py-3 text-sm font-medium text-primary-foreground shadow-[0_0_15px_rgba(59,130,246,0.3)] transition-all hover:bg-primary/90 hover:shadow-[0_0_25px_rgba(59,130,246,0.5)] disabled:opacity-50 disabled:shadow-none sm:w-auto"
                            >
                                {updateProfile.isPending ? t('actions.saving') : t('actions.save')}
                            </button>
                        </div>
                    </section>
                </form>

                {/* Achievements */}
                <AchievementsSection
                    isLoading={achievementsLoading}
                    earnedCount={achievementsData?.unlocked.length ?? 0}
                    unlockedMap={unlockedMap}
                />

                {/* Certificates and Become Instructor */}
                <QuickNavSection isStudent={user?.roles.includes('Student') ?? false} />
            </div>
        </div>
    );
}
