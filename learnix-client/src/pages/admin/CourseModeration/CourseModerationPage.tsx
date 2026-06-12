import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { EyeOff, Eye, Trash2, RefreshCw } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { PAGINATION } from '@/const/ui.constants';
import { cn } from '@/utils/cn';
import type { ManageCourseCardDto, CourseStatus } from '@/types/course.types';

const PAGE_SIZE = PAGINATION.DEFAULT;

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

type PendingAction =
    | { type: 'publish'; course: ManageCourseCardDto }
    | { type: 'unpublish'; course: ManageCourseCardDto }
    | { type: 'delete'; course: ManageCourseCardDto }
    | { type: 'recover'; course: ManageCourseCardDto };

export default function CourseModerationPage() {
    const { t } = useTranslation('admin');
    const qc = useQueryClient();
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [includeDeleted, setIncludeDeleted] = useState(false);
    const [skip, setSkip] = useState(0);
    const [pending, setPending] = useState<PendingAction | null>(null);

    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearch(search);
            setSkip(0);
        }, 400);
        return () => clearTimeout(timer);
    }, [search]);

    const STATUS_LABELS: Record<CourseStatus, string> = {
        Published: t('courseStatusPublished'),
        Draft: t('courseStatusDraft'),
        Archived: t('courseStatusArchived'),
    };

    const filters = {
        search: debouncedSearch || undefined,
        includeDeleted,
        skip,
        take: PAGE_SIZE,
    };

    const { data, isLoading } = useQuery({
        queryKey: queryKeys.admin.courses(filters as Record<string, unknown>),
        queryFn: () => adminApi.getCourses(filters),
    });

    function invalidateCourses() {
        qc.invalidateQueries({ queryKey: queryKeys.admin.coursesList() });
    }

    const publishMutation = useMutation({
        mutationFn: (id: string) => adminApi.publishCourse(id),
        onSuccess: () => {
            toast.success(t('toastPublished'));
            invalidateCourses();
            setPending(null);
        },
    });

    const unpublishMutation = useMutation({
        mutationFn: (id: string) => adminApi.unpublishCourse(id),
        onSuccess: () => {
            toast.success(t('toastUnpublished'));
            invalidateCourses();
            setPending(null);
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => adminApi.deleteCourse(id),
        onSuccess: () => {
            toast.success(t('toastCourseDeleted'));
            invalidateCourses();
            setPending(null);
        },
    });

    const recoverMutation = useMutation({
        mutationFn: (id: string) => adminApi.recoverCourse(id),
        onSuccess: () => {
            toast.success(t('toastCourseRecovered'));
            invalidateCourses();
            setPending(null);
        },
    });

    const courses = data?.items ?? [];
    const totalPages = data?.totalPages ?? 0;
    const currentPage = Math.floor(skip / PAGE_SIZE) + 1;

    const isAnyPending =
        publishMutation.isPending ||
        unpublishMutation.isPending ||
        deleteMutation.isPending ||
        recoverMutation.isPending;

    function handleConfirm() {
        if (!pending) return;
        if (pending.type === 'publish') publishMutation.mutate(pending.course.id);
        else if (pending.type === 'unpublish') unpublishMutation.mutate(pending.course.id);
        else if (pending.type === 'delete') deleteMutation.mutate(pending.course.id);
        else if (pending.type === 'recover') recoverMutation.mutate(pending.course.id);
    }

    function dialogProps(): {
        title: string;
        description: string;
        confirmLabel: string;
        variant: 'destructive' | 'warning' | 'default';
    } | null {
        if (!pending) return null;
        if (pending.type === 'publish')
            return {
                title: t('btnPublish'),
                description: t('confirmPublish', { title: pending.course.title }),
                confirmLabel: t('btnPublish'),
                variant: 'default',
            };
        if (pending.type === 'unpublish')
            return {
                title: t('btnUnpublish'),
                description: t('confirmUnpublish', { title: pending.course.title }),
                confirmLabel: t('btnUnpublish'),
                variant: 'warning',
            };
        if (pending.type === 'delete')
            return {
                title: t('btnDeleteCourse'),
                description: t('confirmDeleteCourse', { title: pending.course.title }),
                confirmLabel: t('btnDeleteCourse'),
                variant: 'destructive',
            };
        if (pending.type === 'recover')
            return {
                title: t('btnRecoverCourse'),
                description: t('confirmRecoverCourse', { title: pending.course.title }),
                confirmLabel: t('btnRecoverCourse'),
                variant: 'default',
            };
        return null;
    }

    const dialog = dialogProps();

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8">
                <h1 className="font-heading text-3xl font-bold text-foreground">
                    {t('coursesTitle')}
                </h1>
                <p className="mt-1 text-muted-foreground">{t('coursesSubtitle')}</p>
            </div>

            {/* Toolbar */}
            <div className="mb-4 flex items-center gap-4">
                <input
                    type="text"
                    placeholder={t('coursesSearch')}
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    className="w-full max-w-sm rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                <label className="flex cursor-pointer items-center gap-2 text-sm text-foreground">
                    <input
                        type="checkbox"
                        checked={includeDeleted}
                        onChange={(e) => {
                            setIncludeDeleted(e.target.checked);
                            setSkip(0);
                        }}
                        className="accent-primary"
                    />
                    {t('coursesShowDeleted')}
                </label>
            </div>

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                {isLoading ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        Loading...
                    </div>
                ) : courses.length === 0 ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        {t('emptyCourses')}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colCourse')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colCourseStatus')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colEnrollments')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">{t('colPrice')}</th>
                                <th className="px-5 py-3 text-right font-medium">
                                    {t('colActions')}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {courses.map((c) => (
                                <tr
                                    key={c.id}
                                    className={cn(
                                        'hover:bg-secondary/30',
                                        c.isDeleted && 'opacity-50',
                                    )}
                                >
                                    {/* Course */}
                                    <td className="px-5 py-3">
                                        <div className="flex items-center gap-3">
                                            <div className="h-10 w-14 shrink-0 overflow-hidden rounded bg-gradient-to-br from-primary/30 to-accent/30">
                                                {c.coverImageUrl && (
                                                    <img
                                                        src={c.coverImageUrl}
                                                        alt=""
                                                        className="h-full w-full object-cover"
                                                    />
                                                )}
                                            </div>
                                            <div>
                                                <p className="font-medium text-foreground">
                                                    {c.title}
                                                </p>
                                                {c.isDeleted && (
                                                    <span className="text-xs text-destructive">
                                                        {t('courseStatusDeleted')}
                                                    </span>
                                                )}
                                            </div>
                                        </div>
                                    </td>

                                    {/* Status */}
                                    <td className="px-5 py-3">
                                        <span
                                            className={cn(
                                                'rounded px-2 py-0.5 text-xs font-medium',
                                                STATUS_STYLES[c.status] ??
                                                    'bg-muted text-muted-foreground',
                                            )}
                                        >
                                            {STATUS_LABELS[c.status] ?? c.status}
                                        </span>
                                    </td>

                                    {/* Enrollments */}
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {c.enrollmentsCount}
                                    </td>

                                    {/* Price */}
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {c.isFree ? t('courseFree') : `$${c.price.toFixed(2)}`}
                                    </td>

                                    {/* Actions */}
                                    <td className="px-5 py-3">
                                        <div className="flex items-center justify-end gap-1">
                                            {!c.isDeleted && c.status === 'Draft' && (
                                                <button
                                                    onClick={() =>
                                                        setPending({ type: 'publish', course: c })
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={t('btnPublish')}
                                                >
                                                    <Eye size={14} />
                                                </button>
                                            )}
                                            {!c.isDeleted && c.status === 'Published' && (
                                                <button
                                                    onClick={() =>
                                                        setPending({ type: 'unpublish', course: c })
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                    title={t('btnUnpublish')}
                                                >
                                                    <EyeOff size={14} />
                                                </button>
                                            )}
                                            {c.isDeleted ? (
                                                <button
                                                    onClick={() =>
                                                        setPending({ type: 'recover', course: c })
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={t('btnRecoverCourse')}
                                                >
                                                    <RefreshCw size={14} />
                                                </button>
                                            ) : (
                                                <button
                                                    onClick={() =>
                                                        setPending({ type: 'delete', course: c })
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                                                    title={t('btnDeleteCourse')}
                                                >
                                                    <Trash2 size={14} />
                                                </button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}

                {/* Pagination */}
                {totalPages > 1 && (
                    <div className="flex items-center justify-between border-t border-border px-5 py-3">
                        <span className="text-sm text-muted-foreground">
                            {t('pageOf', { page: currentPage, total: totalPages })}
                        </span>
                        <div className="flex gap-2">
                            <button
                                onClick={() => setSkip(Math.max(0, skip - PAGE_SIZE))}
                                disabled={skip === 0}
                                className="rounded px-3 py-1 text-sm text-foreground transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
                            >
                                {t('prev')}
                            </button>
                            <button
                                onClick={() => setSkip(skip + PAGE_SIZE)}
                                disabled={currentPage >= totalPages}
                                className="rounded px-3 py-1 text-sm text-foreground transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
                            >
                                {t('next')}
                            </button>
                        </div>
                    </div>
                )}
            </div>

            {/* Confirm dialog */}
            {pending && dialog && (
                <ConfirmDialog
                    title={dialog.title}
                    description={dialog.description}
                    confirmLabel={dialog.confirmLabel}
                    variant={dialog.variant}
                    isPending={isAnyPending}
                    onConfirm={handleConfirm}
                    onClose={() => setPending(null)}
                />
            )}
        </div>
    );
}
