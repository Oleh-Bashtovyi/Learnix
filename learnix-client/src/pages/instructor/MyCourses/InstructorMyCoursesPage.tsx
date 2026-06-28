import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { Pagination } from '@/components/common/Pagination';
import { PAGINATION } from '@/const/ui.constants';
import {
    useArchiveCourse,
    useDeleteCourse,
    usePublishCourse,
    useUnarchiveCourse,
    useUnpublishCourse,
} from '@/hooks/useCourseMutations';
import { useMyCoursesQuery } from '@/hooks/useMyCoursesQuery';
import type { ManageCourseCardDto } from '@/types/course.types';
import { InstructorCourseRow } from './components/InstructorCourseRow';

const PAGE_SIZE = PAGINATION.DEFAULT;

type PendingAction =
    | { type: 'archive'; course: ManageCourseCardDto }
    | { type: 'delete'; course: ManageCourseCardDto };

export default function InstructorMyCoursesPage() {
    const { t } = useTranslation('instructor');
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
                                <InstructorCourseRow
                                    key={course.id}
                                    course={course}
                                    onArchive={(c) => setPending({ type: 'archive', course: c })}
                                    onDelete={(c) => setPending({ type: 'delete', course: c })}
                                    publishMutation={publishMutation}
                                    unpublishMutation={unpublishMutation}
                                    unarchiveMutation={unarchiveMutation}
                                />
                            ))}
                        </tbody>
                    </table>
                )}

                {/* Pagination */}
                <Pagination
                    page={currentPage}
                    totalPages={totalPages}
                    onChange={(p) => setSkip((p - 1) * PAGE_SIZE)}
                    prevLabel={t('paginationPrev')}
                    nextLabel={t('paginationNext')}
                    className="border-t border-border px-5 py-3"
                />
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
