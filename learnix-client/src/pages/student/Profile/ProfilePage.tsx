import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import {
    User,
    Camera,
    Construction,
    Award,
    ChevronRight,
    CheckCircle2,
    AlertTriangle,
    GraduationCap,
    Mail,
} from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { profileSchema, type ProfileFormValues } from '@/schemas/profile.schema';
import { useMyProfile } from '@/hooks/useMyProfile';
import { useUpdateProfile } from '@/hooks/useUpdateProfile';
import { useRequestUploadUrl } from '@/hooks/useRequestUploadUrl';
import { useMyAchievements } from '@/hooks/useMyAchievements';
import { useAuthStore } from '@/store/auth.store';
import { authApi } from '@/api/auth.api';
import { ALL_ACHIEVEMENT_CODES } from '@/const/localization/achievements';
import { AchievementBadge } from '@/components/common/AchievementBadge';
import { cn } from '@/utils/cn';
import { isValidationError } from '@/utils/errors';

export default function ProfilePage() {
    const { t } = useTranslation('profile');
    const { t: tEmail } = useTranslation('emailConfirmation');
    const { data: profile, isLoading } = useMyProfile();
    const updateProfile = useUpdateProfile();
    const { uploadFile, isUploading } = useRequestUploadUrl();
    const { data: achievementsData, isLoading: achievementsLoading } = useMyAchievements();
    const user = useAuthStore((s) => s.user);
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
            setResendCooldown(60);
            toast.success('Confirmation email sent. Check your inbox.');
        },
        meta: { suppressGlobalError: true },
        onError: () => toast.error('Failed to resend. Please try again later.'),
    });

    const [avatarBlobPath, setAvatarBlobPath] = useState<string | null>(null);
    const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

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
            <div className="mx-auto max-w-4xl px-4 py-12 sm:px-6 md:py-20">
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
        <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 sm:py-12">
            <h1 className="font-heading text-2xl font-bold text-foreground sm:text-3xl">
                {t('pageTitle')}
            </h1>

            <form onSubmit={form.handleSubmit(onSubmit)} className="mt-6 space-y-6 sm:mt-8">
                {/* Profile Information */}
                <section className="rounded-xl border border-border bg-card">
                    <div className="border-b border-border px-4 py-4 sm:px-6">
                        <h2 className="font-heading text-lg font-semibold">
                            {t('sections.personal')}
                        </h2>
                    </div>

                    <div className="flex flex-col gap-8 p-4 sm:p-6 md:flex-row md:items-start">
                        {/* Left column: Avatar */}
                        <div className="flex shrink-0 flex-col items-center text-center md:w-56 md:items-start md:text-left">
                            <div className="relative">
                                <div className="flex h-28 w-28 items-center justify-center overflow-hidden rounded-full border-4 border-background bg-muted shadow-sm md:h-32 md:w-32">
                                    {displayAvatar !== null ? (
                                        <img
                                            src={displayAvatar}
                                            alt="Avatar"
                                            className="h-full w-full object-cover"
                                        />
                                    ) : (
                                        <User className="h-12 w-12 text-muted-foreground md:h-14 md:w-14" />
                                    )}
                                </div>
                                <button
                                    type="button"
                                    onClick={() => fileInputRef.current?.click()}
                                    disabled={isUploading}
                                    title={isUploading ? t('avatar.uploading') : t('avatar.upload')}
                                    className="absolute bottom-1 right-1 flex h-9 w-9 items-center justify-center rounded-full border-2 border-background bg-accent text-accent-foreground shadow-md transition-transform hover:scale-105 hover:opacity-90 disabled:scale-100 disabled:opacity-50"
                                >
                                    <Camera className="h-4 w-4" />
                                </button>
                                <input
                                    ref={fileInputRef}
                                    type="file"
                                    accept="image/jpeg,image/png,image/webp"
                                    className="hidden"
                                    onChange={handleAvatarChange}
                                />
                            </div>
                            <div className="mt-4">
                                <h3 className="font-heading text-lg font-semibold text-foreground">
                                    {profile?.firstName} {profile?.lastName}
                                </h3>
                                <p className="mt-1 max-w-[200px] text-xs text-muted-foreground">
                                    {t('avatar.hint')}
                                </p>
                            </div>
                        </div>

                        {/* Right column: Form Fields */}
                        <div className="flex-1 space-y-5">
                            <div className="grid gap-5 sm:grid-cols-2">
                                <div>
                                    <label className="block text-sm font-medium text-foreground">
                                        {t('fields.firstName')}
                                    </label>
                                    <div className="relative mt-1.5">
                                        <input
                                            {...form.register('firstName')}
                                            className={cn(
                                                'w-full rounded-lg border border-border bg-muted/30 px-4 py-2.5 text-sm outline-none transition-all',
                                                'hover:border-primary/50 focus:border-primary focus:bg-background focus:ring-1 focus:ring-primary',
                                                form.formState.errors.firstName
                                                    ? 'border-destructive'
                                                    : 'border-border',
                                            )}
                                        />
                                    </div>
                                    {form.formState.errors.firstName && (
                                        <p className="mt-1.5 text-xs text-destructive">
                                            {form.formState.errors.firstName.message}
                                        </p>
                                    )}
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-foreground">
                                        {t('fields.lastName')}
                                    </label>
                                    <div className="relative mt-1.5">
                                        <input
                                            {...form.register('lastName')}
                                            className={cn(
                                                'w-full rounded-lg border border-border bg-muted/30 px-4 py-2.5 text-sm outline-none transition-all',
                                                'hover:border-primary/50 focus:border-primary focus:bg-background focus:ring-1 focus:ring-primary',
                                                form.formState.errors.lastName
                                                    ? 'border-destructive'
                                                    : 'border-border',
                                            )}
                                        />
                                    </div>
                                    {form.formState.errors.lastName && (
                                        <p className="mt-1.5 text-xs text-destructive">
                                            {form.formState.errors.lastName.message}
                                        </p>
                                    )}
                                </div>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-foreground">
                                    {t('fields.email')}
                                </label>
                                <div className="relative mt-1.5">
                                    <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                                        <Mail className="h-4 w-4 text-muted-foreground" />
                                    </div>
                                    <input
                                        value={profile?.email ?? ''}
                                        readOnly
                                        className={cn(
                                            'w-full cursor-not-allowed rounded-lg border bg-muted/30 py-2.5 pl-9 pr-9 text-sm text-muted-foreground transition-colors',
                                            user?.emailVerified
                                                ? 'border-success/50'
                                                : 'border-border',
                                        )}
                                    />
                                    {user?.emailVerified && (
                                        <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3">
                                            <div className="flex h-5 w-5 items-center justify-center rounded-full bg-success/20">
                                                <CheckCircle2 className="h-3.5 w-3.5 text-success" />
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
                                                <CheckCircle2 className="h-3.5 w-3.5" />
                                                {tEmail('profile.confirmed')}
                                            </>
                                        ) : (
                                            <>
                                                <AlertTriangle className="h-3.5 w-3.5" />
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
                                        onClick={() => resendEmail()}
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

                            <div>
                                <label className="block text-sm font-medium text-foreground">
                                    {t('fields.bio')}
                                </label>
                                <textarea
                                    {...form.register('bio')}
                                    rows={4}
                                    placeholder={t('fields.bioPlaceholder')}
                                    className={cn(
                                        'mt-1.5 w-full resize-none rounded-lg border border-border bg-muted/30 p-3 text-sm outline-none transition-all',
                                        'hover:border-primary/50 focus:border-primary focus:bg-background focus:ring-1 focus:ring-primary',
                                        form.formState.errors.bio
                                            ? 'border-destructive'
                                            : 'border-border',
                                    )}
                                />
                                {form.formState.errors.bio && (
                                    <p className="mt-1.5 text-xs text-destructive">
                                        {form.formState.errors.bio.message}
                                    </p>
                                )}
                            </div>
                        </div>
                    </div>

                    <div className="flex flex-col justify-end border-t border-border bg-muted/20 px-4 py-4 sm:flex-row sm:px-6">
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

                {/* Preferences placeholder */}
                {/* <section className="rounded-xl border border-border bg-card p-6 opacity-70">
                    <div className="flex items-center gap-3">
                        <h2 className="font-heading text-lg font-semibold">
                            {t('sections.preferences')}
                        </h2>
                        <span className="inline-flex items-center gap-1 rounded-full bg-warning/15 px-2.5 py-0.5 text-xs font-medium text-warning">
                            <Construction className="h-3 w-3" />
                            {t('preferences.comingSoon')}
                        </span>
                    </div>
                    <p className="mt-2 text-sm text-muted-foreground">
                        {t('preferences.description')}
                    </p>
                </section> */}

                {/* Achievements */}
                <section className="rounded-xl border border-border bg-card p-4 sm:p-6">
                    <div className="flex flex-col justify-between gap-2 sm:flex-row sm:items-center sm:gap-0">
                        <h2 className="font-heading text-lg font-semibold">
                            {t('achievements.sectionTitle')}
                        </h2>
                        <span className="text-sm text-muted-foreground">
                            {t('achievements.earnedCount', {
                                earned: achievementsData?.unlocked.length ?? 0,
                                total: ALL_ACHIEVEMENT_CODES.length,
                            })}
                        </span>
                    </div>

                    {achievementsLoading ? (
                        <div className="mt-4 grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5">
                            {Array.from({ length: ALL_ACHIEVEMENT_CODES.length }).map((_, i) => (
                                <div key={i} className="h-24 animate-pulse rounded-xl bg-muted" />
                            ))}
                        </div>
                    ) : (
                        <div className="mt-4 grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5">
                            {ALL_ACHIEVEMENT_CODES.map((code) => {
                                const unlocked = unlockedMap.get(code);
                                return (
                                    <AchievementBadge
                                        key={code}
                                        code={code}
                                        size="sm"
                                        unlockedAt={unlocked?.unlockedAt}
                                        isNew={unlocked ? !unlocked.seen : false}
                                    />
                                );
                            })}
                        </div>
                    )}

                    <div className="mt-4 text-right">
                        <Link
                            to="/achievements"
                            className="text-sm font-medium text-primary hover:underline"
                        >
                            {t('achievements.viewAll')}
                        </Link>
                    </div>
                </section>

                {/* Certificates */}
                <Link
                    to="/certificates"
                    className="flex items-center gap-4 rounded-xl border border-border bg-card p-4 transition-colors hover:bg-secondary sm:p-5"
                >
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
                        <Award className="h-5 w-5 text-primary" />
                    </div>
                    <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium text-foreground">
                            {t('certificatesNav.title')}
                        </p>
                        <p className="text-xs text-muted-foreground">{t('certificatesNav.desc')}</p>
                    </div>
                    <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                </Link>

                {/* Become instructor — only for students */}
                {user?.role === 'Student' && (
                    <Link
                        to="/become-instructor"
                        className="flex items-center gap-4 rounded-xl border border-border bg-card p-4 transition-colors hover:bg-secondary sm:p-5"
                    >
                        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10">
                            <GraduationCap className="h-5 w-5 text-accent" />
                        </div>
                        <div className="min-w-0 flex-1">
                            <p className="text-sm font-medium text-foreground">
                                {t('becomeInstructorNav.title')}
                            </p>
                            <p className="text-xs text-muted-foreground">
                                {t('becomeInstructorNav.desc')}
                            </p>
                        </div>
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                    </Link>
                )}
            </form>
        </div>
    );
}
