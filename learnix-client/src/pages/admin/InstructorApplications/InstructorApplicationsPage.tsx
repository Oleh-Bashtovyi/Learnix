import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { CheckCircle, XCircle, ExternalLink } from 'lucide-react';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { RejectDialog } from './RejectDialog';
import { ADMIN } from '@/const/localization/admin';
import type { PendingApplicationDto } from '@/types/admin.types';

const PAGE_SIZE = 10;

function applicantInitials(a: PendingApplicationDto) {
    return `${a.firstName[0] ?? ''}${a.lastName[0] ?? ''}`.toUpperCase();
}

export default function InstructorApplicationsPage() {
    const qc = useQueryClient();
    const [skip, setSkip] = useState(0);
    const [rejectTarget, setRejectTarget] = useState<PendingApplicationDto | null>(null);

    const params = { skip, take: PAGE_SIZE };

    const { data, isLoading } = useQuery({
        queryKey: queryKeys.admin.applications(params as Record<string, unknown>),
        queryFn: () => adminApi.getPendingApplications(params),
    });

    function invalidateApps() {
        qc.invalidateQueries({ queryKey: queryKeys.admin.applicationsList() });
    }

    const approveMutation = useMutation({
        mutationFn: (id: string) => adminApi.approveApplication(id),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_APPROVED);
            invalidateApps();
        },
    });

    const rejectMutation = useMutation({
        mutationFn: ({ id, reason }: { id: string; reason: string | null }) =>
            adminApi.rejectApplication(id, reason),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_REJECTED);
            setRejectTarget(null);
            invalidateApps();
        },
    });

    const applications = data?.items ?? [];
    const totalPages = data?.totalPages ?? 0;
    const currentPage = Math.floor(skip / PAGE_SIZE) + 1;

    function handleApprove(a: PendingApplicationDto) {
        approveMutation.mutate(a.id);
    }

    function handleRejectConfirm(reason: string | null) {
        if (!rejectTarget) return;
        rejectMutation.mutate({ id: rejectTarget.id, reason });
    }

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8">
                <h1 className="font-heading text-3xl font-bold text-foreground">
                    {ADMIN.APPLICATIONS_TITLE}
                </h1>
                <p className="mt-1 text-muted-foreground">{ADMIN.APPLICATIONS_SUBTITLE}</p>
            </div>

            {isLoading ? (
                <div className="py-16 text-center text-sm text-muted-foreground">Loading...</div>
            ) : applications.length === 0 ? (
                <div className="flex flex-col items-center py-20">
                    <CheckCircle size={48} className="mb-4 text-success" />
                    <p className="font-heading font-semibold text-foreground">
                        {ADMIN.APPLICATIONS_EMPTY}
                    </p>
                    <p className="mt-1 text-sm text-muted-foreground">
                        {ADMIN.APPLICATIONS_EMPTY_SUB}
                    </p>
                </div>
            ) : (
                <>
                    <div className="space-y-4">
                        {applications.map((a) => (
                            <div key={a.id} className="rounded-xl border border-border bg-card p-6">
                                <div className="flex items-start gap-4">
                                    {/* Avatar */}
                                    <div className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-accent/20 font-semibold text-accent">
                                        {applicantInitials(a)}
                                    </div>

                                    {/* Info */}
                                    <div className="min-w-0 flex-1">
                                        <div className="flex flex-wrap items-start justify-between gap-3">
                                            <div>
                                                <p className="font-heading font-semibold text-foreground">
                                                    {a.firstName} {a.lastName}
                                                </p>
                                                <p className="text-sm text-muted-foreground">
                                                    {a.email}
                                                </p>
                                            </div>
                                            <p className="text-xs text-muted-foreground">
                                                {ADMIN.APP_SUBMITTED}{' '}
                                                {new Date(a.submittedAt).toLocaleDateString()}
                                            </p>
                                        </div>

                                        {/* Motivation */}
                                        <div className="mt-3">
                                            <p className="mb-1 text-xs font-medium uppercase tracking-wider text-muted-foreground">
                                                {ADMIN.APP_MOTIVATION_LABEL}
                                            </p>
                                            <p className="line-clamp-3 text-sm leading-relaxed text-foreground">
                                                {a.motivationText}
                                            </p>
                                        </div>

                                        {/* Portfolio */}
                                        {a.portfolioUrl && (
                                            <div className="mt-2">
                                                <p className="mb-1 text-xs font-medium uppercase tracking-wider text-muted-foreground">
                                                    {ADMIN.APP_PORTFOLIO_LABEL}
                                                </p>
                                                <a
                                                    href={a.portfolioUrl}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    className="inline-flex items-center gap-1 text-sm text-primary hover:underline"
                                                >
                                                    {a.portfolioUrl}
                                                    <ExternalLink size={12} />
                                                </a>
                                            </div>
                                        )}

                                        {/* Actions */}
                                        <div className="mt-4 flex gap-2">
                                            <button
                                                onClick={() => handleApprove(a)}
                                                disabled={approveMutation.isPending}
                                                className="flex items-center gap-1.5 rounded-lg bg-success/10 px-4 py-1.5 text-sm font-medium text-success transition-colors hover:bg-success/20 disabled:opacity-50"
                                            >
                                                <CheckCircle size={14} />
                                                {ADMIN.BTN_APPROVE}
                                            </button>
                                            <button
                                                onClick={() => setRejectTarget(a)}
                                                disabled={rejectMutation.isPending}
                                                className="flex items-center gap-1.5 rounded-lg bg-destructive/10 px-4 py-1.5 text-sm font-medium text-destructive transition-colors hover:bg-destructive/20 disabled:opacity-50"
                                            >
                                                <XCircle size={14} />
                                                {ADMIN.BTN_REJECT}
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <div className="mt-6 flex items-center justify-between">
                            <span className="text-sm text-muted-foreground">
                                {ADMIN.PAGE_OF(currentPage, totalPages)}
                            </span>
                            <div className="flex gap-2">
                                <button
                                    onClick={() => setSkip(Math.max(0, skip - PAGE_SIZE))}
                                    disabled={skip === 0}
                                    className="rounded px-3 py-1 text-sm text-foreground transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
                                >
                                    {ADMIN.PREV}
                                </button>
                                <button
                                    onClick={() => setSkip(skip + PAGE_SIZE)}
                                    disabled={currentPage >= totalPages}
                                    className="rounded px-3 py-1 text-sm text-foreground transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
                                >
                                    {ADMIN.NEXT}
                                </button>
                            </div>
                        </div>
                    )}
                </>
            )}

            {/* Reject dialog */}
            {rejectTarget && (
                <RejectDialog
                    applicantName={`${rejectTarget.firstName} ${rejectTarget.lastName}`}
                    onConfirm={handleRejectConfirm}
                    onCancel={() => setRejectTarget(null)}
                    isLoading={rejectMutation.isPending}
                />
            )}
        </div>
    );
}
