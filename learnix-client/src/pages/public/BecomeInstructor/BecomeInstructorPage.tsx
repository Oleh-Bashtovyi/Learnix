import { FormProvider, type UseFormReturn, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { zodResolver } from '@hookform/resolvers/zod';
import { CheckCircle, Clock, ShieldAlert, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { FormInput } from '@/components/common/form/FormInput';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { Seo } from '@/components/common/seo/Seo';
import { INSTRUCTOR_APP_LIMITS } from '@/const/instructor.constants';
import { UserRole } from '@/enums/user.enums';
import { useMyApplication } from '@/hooks/instructor/useMyApplication';
import { useSubmitApplication } from '@/hooks/instructor/useSubmitApplication';
import { APP_ROUTES } from '@/routes/paths';
import {
    type InstructorApplicationFormData,
    instructorApplicationSchema,
} from '@/schemas/instructorApplication.schema';
import { useAuthStore } from '@/store/auth.store';
import { isInstructorOrAdmin } from '@/utils/roles';

export default function BecomeInstructorPage() {
    const { t } = useTranslation('instructor');
    const user = useAuthStore((s) => s.user);
    const { data: application, isLoading } = useMyApplication();
    const submitApplication = useSubmitApplication();

    // The two cards below need to tell instructors and admins apart; the form only cares
    // that neither of them may apply.
    const isInstructor = user?.roles.includes(UserRole.Instructor) ?? false;
    const isAdmin = user?.roles.includes(UserRole.Admin) ?? false;
    const canApply = !!user && !isInstructorOrAdmin(user);

    const form = useForm<InstructorApplicationFormData>({
        resolver: zodResolver(instructorApplicationSchema),
        defaultValues: { motivationText: '', portfolioUrl: '' },
    });

    function onSubmit(data: InstructorApplicationFormData) {
        if (!user?.emailVerified) {
            toast.error(t('emailNotVerified', 'Please confirm your email address first.'));
            return;
        }

        submitApplication.mutate({
            motivationText: data.motivationText,
            portfolioUrl: data.portfolioUrl || undefined,
        });
    }

    return (
        <div className="mx-auto max-w-2xl px-4 py-10 sm:py-16">
            <Seo title={t('becomeTitle')} description={t('becomeSubtitle')} />
            {/* Page title */}
            <div className="mb-8 text-center sm:mb-10">
                <h1 className="font-heading text-3xl font-bold text-foreground sm:text-4xl">
                    {t('becomeTitle')}
                </h1>
                <p className="mt-3 text-base text-muted-foreground sm:text-lg">
                    {t('becomeSubtitle')}
                </p>
            </div>

            {/* Not logged in */}
            {!user && (
                <div className="rounded-xl border border-border bg-card p-8 text-center">
                    <h2 className="mb-2 font-heading text-xl font-semibold">
                        {t('notLoggedInTitle')}
                    </h2>
                    <p className="mb-6 text-muted-foreground">{t('notLoggedInBody')}</p>
                    <Link
                        to={
                            APP_ROUTES.public.login +
                            '?redirect=' +
                            APP_ROUTES.public.becomeInstructor
                        }
                        className="inline-block rounded-lg bg-primary px-6 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        {t('btnSignIn')}
                    </Link>
                </div>
            )}

            {/* Already an instructor */}
            {isInstructor && (
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

            {/* Admin — the API rejects their applications; they can self-assign the role instead */}
            {isAdmin && !isInstructor && (
                <div className="rounded-xl border border-border bg-card p-8 text-center">
                    <ShieldAlert className="mx-auto mb-3 text-warning" size={40} />
                    <h2 className="mb-2 font-heading text-xl font-semibold">
                        {t('adminCannotApplyTitle')}
                    </h2>
                    <p className="text-muted-foreground">{t('adminCannotApplyBody')}</p>
                </div>
            )}

            {/* Loading application status */}
            {canApply && isLoading && (
                <div className="py-16 text-center text-sm text-muted-foreground">
                    {t('common:status.loading')}
                </div>
            )}

            {/* Pending application */}
            {canApply && !isLoading && application?.status === 'Pending' && (
                <div className="rounded-xl border border-border bg-card p-8 text-center">
                    <Clock className="mx-auto mb-3 text-warning" size={40} />
                    <h2 className="mb-2 font-heading text-xl font-semibold">
                        {t('applicationPendingTitle')}
                    </h2>
                    <p className="text-muted-foreground">{t('applicationPendingBody')}</p>
                </div>
            )}

            {/* Approved, but the role is not in the token yet */}
            {canApply && !isLoading && application?.status === 'Approved' && (
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
            {canApply && !isLoading && application?.status === 'Rejected' && (
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
                        form={form}
                        onSubmit={onSubmit}
                        isPending={submitApplication.isPending}
                        submitLabel={t('btnResubmit')}
                    />
                </div>
            )}

            {/* No application yet */}
            {canApply && !isLoading && !application && (
                <ApplicationForm
                    form={form}
                    onSubmit={onSubmit}
                    isPending={submitApplication.isPending}
                    submitLabel={t('btnSubmitApplication')}
                />
            )}
        </div>
    );
}

interface ApplicationFormProps {
    form: UseFormReturn<InstructorApplicationFormData>;
    onSubmit: (data: InstructorApplicationFormData) => void;
    isPending: boolean;
    submitLabel: string;
}

function ApplicationForm({ form, onSubmit, isPending, submitLabel }: ApplicationFormProps) {
    const { t } = useTranslation('instructor');
    const {
        register,
        handleSubmit,
        formState: { errors },
    } = form;

    return (
        // FormProvider so the char counters inside the fields can read the live field values.
        <FormProvider {...form}>
            <form
                onSubmit={handleSubmit(onSubmit)}
                className="space-y-5 rounded-xl border border-border bg-card p-5 sm:p-8"
            >
                <FormTextarea
                    label={t('fieldMotivation')}
                    rows={8}
                    maxLength={INSTRUCTOR_APP_LIMITS.MOTIVATION_MAX}
                    showCharLimit
                    placeholder={t('fieldMotivationPlaceholder')}
                    error={errors.motivationText?.message}
                    {...register('motivationText')}
                />

                <FormInput
                    label={t('fieldPortfolio')}
                    type="url"
                    maxLength={INSTRUCTOR_APP_LIMITS.PORTFOLIO_URL_MAX}
                    showCharLimit
                    placeholder={t('fieldPortfolioPlaceholder')}
                    error={errors.portfolioUrl?.message}
                    {...register('portfolioUrl')}
                />

                <button
                    type="submit"
                    disabled={isPending}
                    className="w-full rounded-lg bg-primary py-2.5 font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-60"
                >
                    {isPending ? '...' : submitLabel}
                </button>
            </form>
        </FormProvider>
    );
}
