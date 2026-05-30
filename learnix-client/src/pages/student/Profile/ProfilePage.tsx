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
} from 'lucide-react';
import { toast } from 'sonner';
import { profileSchema, type ProfileFormValues } from '@/schemas/profile.schema';
import { useMyProfile } from '@/hooks/useMyProfile';
import { useUpdateProfile } from '@/hooks/useUpdateProfile';
import { useRequestUploadUrl } from '@/hooks/useRequestUploadUrl';
import { useMyAchievements } from '@/hooks/useMyAchievements';
import { useAuthStore } from '@/store/auth.store';
import { authApi } from '@/api/auth.api';
import { PROFILE } from '@/const/localization/profile';
import { EMAIL_CONFIRMATION } from '@/const/localization/emailConfirmation';
import { ALL_ACHIEVEMENT_CODES } from '@/const/localization/achievements';
import { AchievementBadge } from '@/components/common/AchievementBadge';
import { cn } from '@/utils/cn';
import { isValidationError } from '@/utils/errors';

const TE = EMAIL_CONFIRMATION.PROFILE;

export default function ProfilePage() {
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
            toast.success(PROFILE.MESSAGES.SAVE_SUCCESS);
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
            <div className="mx-auto max-w-3xl px-6 py-20">
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
        <div className="mx-auto max-w-3xl px-6 py-12">
            <h1 className="font-heading text-3xl font-bold text-foreground">
                {PROFILE.PAGE_TITLE}
            </h1>

            <form onSubmit={form.handleSubmit(onSubmit)} className="mt-8 space-y-6">
                {/* Avatar */}
                <section className="rounded-xl border border-border bg-card p-6">
                    <h2 className="font-heading text-lg font-semibold">
                        {PROFILE.SECTIONS.AVATAR}
                    </h2>
                    <div className="mt-4 flex items-center gap-6">
                        <div className="relative">
                            <div className="flex h-24 w-24 items-center justify-center overflow-hidden rounded-full bg-muted">
                                {displayAvatar !== null ? (
                                    <img
                                        src={displayAvatar}
                                        alt="Avatar"
                                        className="h-full w-full object-cover"
                                    />
                                ) : (
                                    <User className="h-10 w-10 text-muted-foreground" />
                                )}
                            </div>
                            <button
                                type="button"
                                onClick={() => fileInputRef.current?.click()}
                                disabled={isUploading}
                                className="absolute -bottom-1 -right-1 flex h-8 w-8 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-md transition-opacity hover:opacity-90 disabled:opacity-50"
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
                        <div>
                            <button
                                type="button"
                                onClick={() => fileInputRef.current?.click()}
                                disabled={isUploading}
                                className="text-sm font-medium text-primary hover:underline disabled:opacity-50"
                            >
                                {isUploading ? PROFILE.AVATAR.UPLOADING : PROFILE.AVATAR.UPLOAD}
                            </button>
                            <p className="mt-1 text-xs text-muted-foreground">
                                {PROFILE.AVATAR.HINT}
                            </p>
                        </div>
                    </div>
                </section>

                {/* Personal info */}
                <section className="rounded-xl border border-border bg-card p-6">
                    <h2 className="font-heading text-lg font-semibold">
                        {PROFILE.SECTIONS.PERSONAL}
                    </h2>
                    <div className="mt-4 grid gap-4 sm:grid-cols-2">
                        <div>
                            <label className="block text-sm font-medium text-foreground">
                                {PROFILE.FIELDS.FIRST_NAME}
                            </label>
                            <input
                                {...form.register('firstName')}
                                className={cn(
                                    'mt-1 w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none transition-colors',
                                    'focus:border-primary focus:ring-1 focus:ring-primary',
                                    form.formState.errors.firstName
                                        ? 'border-destructive'
                                        : 'border-border',
                                )}
                            />
                            {form.formState.errors.firstName && (
                                <p className="mt-1 text-xs text-destructive">
                                    {form.formState.errors.firstName.message}
                                </p>
                            )}
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-foreground">
                                {PROFILE.FIELDS.LAST_NAME}
                            </label>
                            <input
                                {...form.register('lastName')}
                                className={cn(
                                    'mt-1 w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none transition-colors',
                                    'focus:border-primary focus:ring-1 focus:ring-primary',
                                    form.formState.errors.lastName
                                        ? 'border-destructive'
                                        : 'border-border',
                                )}
                            />
                            {form.formState.errors.lastName && (
                                <p className="mt-1 text-xs text-destructive">
                                    {form.formState.errors.lastName.message}
                                </p>
                            )}
                        </div>
                    </div>

                    <div className="mt-4">
                        <label className="block text-sm font-medium text-foreground">
                            {PROFILE.FIELDS.EMAIL}
                        </label>
                        <input
                            value={profile?.email ?? ''}
                            readOnly
                            className="mt-1 w-full cursor-not-allowed rounded-lg border border-border bg-muted px-3 py-2 text-sm text-muted-foreground"
                        />
                        {user && (
                            <p
                                className={cn(
                                    'mt-1.5 flex items-center gap-1.5 text-xs',
                                    user.emailVerified
                                        ? 'text-success'
                                        : 'text-amber-600 dark:text-amber-400',
                                )}
                            >
                                {user.emailVerified ? (
                                    <>
                                        <CheckCircle2 className="h-3.5 w-3.5" />
                                        {TE.CONFIRMED}
                                    </>
                                ) : (
                                    <>
                                        <AlertTriangle className="h-3.5 w-3.5" />
                                        {TE.NOT_CONFIRMED}
                                    </>
                                )}
                            </p>
                        )}
                    </div>

                    {user && !user.emailVerified && (
                        <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/50 dark:bg-amber-950/30">
                            <p className="text-sm text-amber-800 dark:text-amber-300">
                                {TE.NOT_CONFIRMED_HINT}
                            </p>
                            <button
                                type="button"
                                onClick={() => resendEmail()}
                                disabled={resendCooldown > 0 || isResending}
                                className="mt-3 rounded-lg border border-amber-300 bg-white px-4 py-2 text-sm font-medium text-amber-800 transition-colors hover:bg-amber-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-amber-800 dark:bg-transparent dark:text-amber-300"
                            >
                                {resendCooldown > 0
                                    ? TE.RESEND_COOLDOWN.replace(
                                          '{seconds}',
                                          String(resendCooldown),
                                      )
                                    : TE.RESEND}
                            </button>
                        </div>
                    )}

                    <div className="mt-4">
                        <label className="block text-sm font-medium text-foreground">
                            {PROFILE.FIELDS.BIO}
                        </label>
                        <textarea
                            {...form.register('bio')}
                            rows={4}
                            placeholder={PROFILE.FIELDS.BIO_PLACEHOLDER}
                            className={cn(
                                'mt-1 w-full resize-none rounded-lg border bg-background px-3 py-2 text-sm outline-none transition-colors',
                                'focus:border-primary focus:ring-1 focus:ring-primary',
                                form.formState.errors.bio ? 'border-destructive' : 'border-border',
                            )}
                        />
                        {form.formState.errors.bio && (
                            <p className="mt-1 text-xs text-destructive">
                                {form.formState.errors.bio.message}
                            </p>
                        )}
                    </div>

                    <div className="mt-6 flex justify-end">
                        <button
                            type="submit"
                            disabled={updateProfile.isPending || isUploading}
                            className="rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
                        >
                            {updateProfile.isPending
                                ? PROFILE.ACTIONS.SAVING
                                : PROFILE.ACTIONS.SAVE}
                        </button>
                    </div>
                </section>

                {/* Preferences placeholder */}
                <section className="rounded-xl border border-border bg-card p-6 opacity-70">
                    <div className="flex items-center gap-3">
                        <h2 className="font-heading text-lg font-semibold">
                            {PROFILE.SECTIONS.PREFERENCES}
                        </h2>
                        <span className="inline-flex items-center gap-1 rounded-full bg-warning/15 px-2.5 py-0.5 text-xs font-medium text-warning">
                            <Construction className="h-3 w-3" />
                            {PROFILE.PREFERENCES.COMING_SOON}
                        </span>
                    </div>
                    <p className="mt-2 text-sm text-muted-foreground">
                        {PROFILE.PREFERENCES.DESCRIPTION}
                    </p>
                </section>

                {/* Achievements */}
                <section className="rounded-xl border border-border bg-card p-6">
                    <div className="flex items-center justify-between">
                        <h2 className="font-heading text-lg font-semibold">
                            {PROFILE.ACHIEVEMENTS.SECTION_TITLE}
                        </h2>
                        <span className="text-sm text-muted-foreground">
                            {PROFILE.ACHIEVEMENTS.EARNED_COUNT(
                                achievementsData?.unlocked.length ?? 0,
                                ALL_ACHIEVEMENT_CODES.length,
                            )}
                        </span>
                    </div>

                    {achievementsLoading ? (
                        <div className="mt-4 grid grid-cols-4 gap-3 sm:grid-cols-5">
                            {Array.from({ length: ALL_ACHIEVEMENT_CODES.length }).map((_, i) => (
                                <div key={i} className="h-24 animate-pulse rounded-xl bg-muted" />
                            ))}
                        </div>
                    ) : (
                        <div className="mt-4 grid grid-cols-4 gap-3 sm:grid-cols-5">
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
                            {PROFILE.ACHIEVEMENTS.VIEW_ALL}
                        </Link>
                    </div>
                </section>

                {/* Certificates */}
                <Link
                    to="/certificates"
                    className="flex items-center gap-4 rounded-xl border border-border bg-card p-5 transition-colors hover:bg-secondary"
                >
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
                        <Award className="h-5 w-5 text-primary" />
                    </div>
                    <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium text-foreground">
                            {PROFILE.CERTIFICATES_NAV.TITLE}
                        </p>
                        <p className="text-xs text-muted-foreground">
                            {PROFILE.CERTIFICATES_NAV.DESC}
                        </p>
                    </div>
                    <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                </Link>

                {/* Become instructor — only for students */}
                {user?.role === 'Student' && (
                    <Link
                        to="/become-instructor"
                        className="flex items-center gap-4 rounded-xl border border-border bg-card p-5 transition-colors hover:bg-secondary"
                    >
                        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10">
                            <GraduationCap className="h-5 w-5 text-accent" />
                        </div>
                        <div className="min-w-0 flex-1">
                            <p className="text-sm font-medium text-foreground">
                                {PROFILE.BECOME_INSTRUCTOR_NAV.TITLE}
                            </p>
                            <p className="text-xs text-muted-foreground">
                                {PROFILE.BECOME_INSTRUCTOR_NAV.DESC}
                            </p>
                        </div>
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                    </Link>
                )}
            </form>
        </div>
    );
}
