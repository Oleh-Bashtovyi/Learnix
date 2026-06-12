import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Pencil, Globe, EyeOff, Archive, ArchiveRestore, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import { useMyCoursesQuery } from '@/hooks/useMyCoursesQuery';
import {
    useDeleteCourse,
    usePublishCourse,
    useUnpublishCourse,
    useArchiveCourse,
    useUnarchiveCourse,
} from '@/hooks/useCourseMutations';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { PAGINATION } from '@/const/ui.constants';
import type { ManageCourseCardDto, CourseStatus } from '@/types/course.types';

const PAGE_SIZE = PAGINATION.DEFAULT;

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

type PendingAction =
    | { type: 'archive'; course: ManageCourseCardDto }
    | { type: 'delete'; course: ManageCourseCardDto };

export default function InstructorMyCoursesPage() {
    const { t } = useTranslation('instructor');
    const navigate = useNavigate();
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [skip, setSkip] = useState(0);
    const [pending, setPending] = useState<PendingAction | null>(null);

    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearch(search);
            setSkip(0);
        }, 400);
        return () => clearTimeout(timer);
    }, [search]);

    const { data, isLoading } = useMyCoursesQuery({
        search: debouncedSearch || undefined,
        skip,
        take: PAGE_SIZE,
    });

    const publishMutation = usePublishCourse();
    const unpublishMutation = useUnpublishCourse();
    const archiveMutation = useArchiveCourse();
    const unarchiveMutation = useUnarchiveCourse();
    const deleteMutation = useDeleteCourse();

    const courses = data?.items ?? [];
    const totalPages = data?.totalPages ?? 0;
    const currentPage = Math.floor(skip / PAGE_SIZE) + 1;

    const isAnyPending = archiveMutation.isPending || deleteMutation.isPending;

    const STATUS_LABELS: Record<CourseStatus, string> = {
        Published: t('statusPublished'),
        Draft: t('statusDraft'),
        Archived: t('statusArchived'),
    };

    function handleConfirm() {
        if (!pending) return;
        const closeDialog = { onSuccess: () => setPending(null) };
        if (pending.type === 'archive') archiveMutation.mutate(pending.course.id, closeDialog);
        else if (pending.type === 'delete') deleteMutation.mutate(pending.course.id, closeDialog);
    }

    const dialogProps = pending
        ? pending.type === 'archive'
            ? {
                  title: t('btnArchive'),
                  description: t('confirmArchive', { title: pending.course.title }),
                  confirmLabel: t('btnArchive'),
                  variant: 'warning' as const,
              }
            : {
                  title: t('btnDelete'),
                  description: t('confirmDelete', { title: pending.course.title }),
                  confirmLabel: t('btnDelete'),
                  variant: 'destructive' as const,
              }
        : null;

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8 flex items-end justify-between">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {t('myCoursesTitle')}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{t('myCoursesSubtitle')}</p>
                </div>
                <Link
                    to="/instructor/courses/new"
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                >
                    {t('btnNewCourse')}
                </Link>
            </div>

            {/* Search */}
            <div className="mb-4">
                <input
                    type="text"
                    placeholder={t('myCoursesSearch')}
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    className="w-full max-w-sm rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                {isLoading ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        Loading courses...
                    </div>
                ) : courses.length === 0 ? (
                    <div className="py-16 text-center">
                        <p className="text-sm text-muted-foreground">
                            {debouncedSearch ? t('myCoursesEmpty') : t('myCoursesEmptyDefault')}
                        </p>
                        {!debouncedSearch && (
                            <Link
                                to="/instructor/courses/new"
                                className="mt-3 inline-block text-sm text-primary hover:underline"
                            >
                                {t('dashboardEmptyCta')}
                            </Link>
                        )}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colCourse')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colStatus')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colStudents')}
                                </th>
                                <th className="px-5 py-3 text-right font-medium">
                                    {t('colActions')}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {courses.map((course) => (
                                <tr key={course.id} className="hover:bg-secondary/30">
                                    <td className="px-5 py-3">
                                        <div className="flex items-center gap-3">
                                            <div className="h-10 w-14 shrink-0 overflow-hidden rounded bg-gradient-to-br from-primary/30 to-accent/30">
                                                {course.coverImageUrl && (
                                                    <img
                                                        src={course.coverImageUrl}
                                                        alt=""
                                                        className="h-full w-full object-cover"
                                                    />
                                                )}
                                            </div>
                                            <span className="font-medium text-foreground">
                                                {course.title}
                                            </span>
                                        </div>
                                    </td>
                                    <td className="px-5 py-3">
                                        <span
                                            className={cn(
                                                'rounded px-2 py-0.5 text-xs font-medium',
                                                STATUS_STYLES[course.status],
                                            )}
                                        >
                                            {STATUS_LABELS[course.status]}
                                        </span>
                                    </td>
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {course.enrollmentsCount}
                                    </td>
                                    <td className="px-5 py-3">
                                        <div className="flex items-center justify-end gap-1">
                                            <button
                                                onClick={() =>
                                                    navigate(
                                                        `/instructor/courses/${course.id}/edit`,
                                                    )
                                                }
                                                className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-primary"
                                                title={t('btnEdit')}
                                            >
                                                <Pencil size={14} />
                                            </button>
                                            {course.status === 'Draft' && (
                                                <button
                                                    onClick={() =>
                                                        publishMutation.mutate(course.id)
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={t('btnPublish')}
                                                >
                                                    <Globe size={14} />
                                                </button>
                                            )}
                                            {course.status === 'Published' && (
                                                <button
                                                    onClick={() =>
                                                        unpublishMutation.mutate(course.id)
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                    title={t('btnUnpublish')}
                                                >
                                                    <EyeOff size={14} />
                                                </button>
                                            )}
                                            {course.status !== 'Archived' && (
                                                <button
                                                    onClick={() =>
                                                        setPending({ type: 'archive', course })
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                    title={t('btnArchive')}
                                                >
                                                    <Archive size={14} />
                                                </button>
                                            )}
                                            {course.status === 'Archived' && (
                                                <button
                                                    onClick={() =>
                                                        unarchiveMutation.mutate(course.id)
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={t('btnUnarchive')}
                                                >
                                                    <ArchiveRestore size={14} />
                                                </button>
                                            )}
                                            <button
                                                onClick={() =>
                                                    setPending({ type: 'delete', course })
                                                }
                                                className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                                                title={t('btnDelete')}
                                            >
                                                <Trash2 size={14} />
                                            </button>
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
                            {t('paginationPage', { current: currentPage, total: totalPages })}
                        </span>
                        <div className="flex gap-2">
                            <button
                                onClick={() => setSkip(Math.max(0, skip - PAGE_SIZE))}
                                disabled={skip === 0}
                                className="rounded px-3 py-1 text-sm text-foreground transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
                            >
                                {t('paginationPrev')}
                            </button>
                            <button
                                onClick={() => setSkip(skip + PAGE_SIZE)}
                                disabled={currentPage >= totalPages}
                                className="rounded px-3 py-1 text-sm text-foreground transition-colors hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
                            >
                                {t('paginationNext')}
                            </button>
                        </div>
                    </div>
                )}
            </div>

            {/* Confirm dialog */}
            {pending && dialogProps && (
                <ConfirmDialog
                    title={dialogProps.title}
                    description={dialogProps.description}
                    confirmLabel={dialogProps.confirmLabel}
                    variant={dialogProps.variant}
                    isPending={isAnyPending}
                    onConfirm={handleConfirm}
                    onClose={() => setPending(null)}
                />
            )}
        </div>
    );
}
