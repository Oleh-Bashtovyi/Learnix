import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { CheckCircle, Clock, XCircle } from 'lucide-react';
import { useMyApplication } from '@/hooks/instructor/useMyApplication';
import { useSubmitApplication } from '@/hooks/instructor/useSubmitApplication';
import { APP_ROUTES } from '@/routes/paths';
import {
    type InstructorApplicationFormData,
    instructorApplicationSchema,
} from '@/schemas/instructorApplication.schema';
import { useAuthStore } from '@/store/auth.store';

export default function BecomeInstructorPage() {
    const { t } = useTranslation('instructor');
    const user = useAuthStore((s) => s.user);
    const { data: application, isLoading } = useMyApplication();
    const submitApplication = useSubmitApplication();

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<InstructorApplicationFormData>({
        resolver: zodResolver(instructorApplicationSchema),
    });

    function onSubmit(data: InstructorApplicationFormData) {
        submitApplication.mutate({
            motivationText: data.motivationText,
            portfolioUrl: data.portfolioUrl || undefined,
        });
    }

    return (
        <div className="mx-auto max-w-2xl px-4 py-16">
            {/* Page title */}
            <div className="mb-10 text-center">
                <h1 className="font-heading text-4xl font-bold text-foreground">
                    {t('becomeTitle')}
                </h1>
                <p className="mt-3 text-lg text-muted-foreground">{t('becomeSubtitle')}</p>
            </div>

            {/* Not logged in */}
            {!user && (
                <div className="rounded-xl border border-border bg-card p-8 text-center">
                    <h2 className="mb-2 font-heading text-xl font-semibold">
                        {t('notLoggedInTitle')}
                    </h2>
                    <p className="mb-6 text-muted-foreground">{t('notLoggedInBody')}</p>
                    <Link
                        to="/login?redirect=/become-instructor"
                        className="inline-block rounded-lg bg-primary px-6 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        {t('btnSignIn')}
                    </Link>
                </div>
            )}

            {/* Already an instructor */}
            {user?.roles.includes('Instructor') && (
                <div className="rounded-xl border border-border bg-card p-8 text-center">
                    <CheckCircle className="mx-auto mb-3 text-success" size={40} />
                    <h2 className="mb-2 font-heading text-xl font-semibold">
                        {t('alreadyInstructorTitle')}
                    </h2>
                    <p className="mb-6 text-muted-foreground">{t('alreadyInstructorBody')}</p>
                    <Link
                        to={APP_ROUTES.instructor.dashboard}
                        className="inline-block rounded-lg bg-primary px-6 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        {t('btnGoToDashboard')}
                    </Link>
                </div>
            )}

            {/* Loading application status */}
            {user && user.roles.includes('Student') && isLoading && (
                <div className="py-16 text-center text-sm text-muted-foreground">Loading...</div>
            )}

            {/* Pending application */}
            {user &&
                user.roles.includes('Student') &&
                !isLoading &&
                application?.status === 'Pending' && (
                    <div className="rounded-xl border border-border bg-card p-8 text-center">
                        <Clock className="mx-auto mb-3 text-warning" size={40} />
                        <h2 className="mb-2 font-heading text-xl font-semibold">
                            {t('applicationPendingTitle')}
                        </h2>
                        <p className="text-muted-foreground">{t('applicationPendingBody')}</p>
                    </div>
                )}

            {/* Approved */}
            {user &&
                user.roles.includes('Student') &&
                !isLoading &&
                application?.status === 'Approved' && (
                    <div className="rounded-xl border border-border bg-card p-8 text-center">
                        <CheckCircle className="mx-auto mb-3 text-success" size={40} />
                        <h2 className="mb-2 font-heading text-xl font-semibold">
                            {t('applicationApprovedTitle')}
                        </h2>
                        <p className="mb-6 text-muted-foreground">{t('applicationApprovedBody')}</p>
                        <Link
                            to={APP_ROUTES.instructor.dashboard}
                            className="inline-block rounded-lg bg-primary px-6 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                        >
                            {t('btnGoToDashboard')}
                        </Link>
                    </div>
                )}

            {/* Rejected */}
            {user &&
                user.roles.includes('Student') &&
                !isLoading &&
                application?.status === 'Rejected' && (
                    <div className="space-y-6">
                        <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-6">
                            <div className="flex items-start gap-3">
                                <XCircle className="mt-0.5 shrink-0 text-destructive" size={20} />
                                <div>
                                    <h2 className="font-semibold text-foreground">
                                        {t('applicationRejectedTitle')}
                                    </h2>
                                    {application.rejectionReason && (
                                        <p className="mt-1 text-sm text-muted-foreground">
                                            <span className="font-medium">
                                                {t('applicationRejectedReasonLabel')}
                                            </span>{' '}
                                            {application.rejectionReason}
                                        </p>
                                    )}
                                </div>
                            </div>
                        </div>
                        <ApplicationForm
                            defaultValues={{
                                motivationText: application.motivationText,
                                portfolioUrl: application.portfolioUrl ?? '',
                            }}
                            register={register}
                            handleSubmit={handleSubmit}
                            errors={errors}
                            onSubmit={onSubmit}
                            isPending={submitApplication.isPending}
                            submitLabel={t('btnResubmit')}
                        />
                    </div>
                )}

            {/* No application yet */}
            {user && user.roles.includes('Student') && !isLoading && !application && (
                <ApplicationForm
                    register={register}
                    handleSubmit={handleSubmit}
                    errors={errors}
                    onSubmit={onSubmit}
                    isPending={submitApplication.isPending}
                    submitLabel={t('btnSubmitApplication')}
                />
            )}
        </div>
    );
}

interface ApplicationFormProps {
    defaultValues?: { motivationText: string; portfolioUrl: string };
    register: ReturnType<typeof useForm<InstructorApplicationFormData>>['register'];
    handleSubmit: ReturnType<typeof useForm<InstructorApplicationFormData>>['handleSubmit'];
    errors: ReturnType<typeof useForm<InstructorApplicationFormData>>['formState']['errors'];
    onSubmit: (data: InstructorApplicationFormData) => void;
    isPending: boolean;
    submitLabel: string;
}

function ApplicationForm({
    register,
    handleSubmit,
    errors,
    onSubmit,
    isPending,
    submitLabel,
}: ApplicationFormProps) {
    const { t } = useTranslation('instructor');

    return (
        <form
            onSubmit={handleSubmit(onSubmit)}
            className="space-y-5 rounded-xl border border-border bg-card p-8"
        >
            <div>
                <label className="mb-1 block text-sm font-medium text-foreground">
                    {t('fieldMotivation')}
                </label>
                <textarea
                    {...register('motivationText')}
                    rows={6}
                    placeholder={t('fieldMotivationPlaceholder')}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.motivationText && (
                    <p className="mt-1 text-xs text-destructive">{errors.motivationText.message}</p>
                )}
            </div>

            <div>
                <label className="mb-1 block text-sm font-medium text-foreground">
                    {t('fieldPortfolio')}
                </label>
                <input
                    {...register('portfolioUrl')}
                    type="url"
                    placeholder={t('fieldPortfolioPlaceholder')}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.portfolioUrl && (
                    <p className="mt-1 text-xs text-destructive">{errors.portfolioUrl.message}</p>
                )}
            </div>

            <button
                type="submit"
                disabled={isPending}
                className="w-full rounded-lg bg-primary py-2.5 font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-60"
            >
                {isPending ? '...' : submitLabel}
            </button>
        </form>
    );
}
