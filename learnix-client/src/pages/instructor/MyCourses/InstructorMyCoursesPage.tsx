import { type ReactNode, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { PageSizeSelect } from '@/components/common/ui/PageSizeSelect';
import { Pagination } from '@/components/common/ui/Pagination';
import { SearchInput } from '@/components/common/ui/SearchInput';
import { TextLink } from '@/components/common/ui/TextLink';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { PAGINATION } from '@/const/ui.constants';
import {
    useArchiveCourse,
    useDeleteCourse,
    usePublishCourse,
    useUnarchiveCourse,
    useUnpublishCourse,
} from '@/hooks/instructor/useCourseMutations';
import { useMyCoursesQuery } from '@/hooks/instructor/useMyCoursesQuery';
import { APP_ROUTES } from '@/routes/paths';
import type { ManageCourseCardDto } from '@/types/course.types';
import { InstructorCourseRow } from './components/InstructorCourseRow';

const DEFAULT_PAGE_SIZE = PAGINATION.DEFAULT;

/** Placeholder rows shown while the page loads; the value is the key, not an index. */
const SKELETON_ROWS = ['s1', 's2', 's3'];
type PendingAction =
    | { type: 'archive'; course: ManageCourseCardDto }
    | { type: 'delete'; course: ManageCourseCardDto };

export default function InstructorMyCoursesPage() {
    const { t } = useTranslation('instructor');
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [skip, setSkip] = useState(0);
    const [pageSize, setPageSize] = useState<number>(DEFAULT_PAGE_SIZE);
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
        take: pageSize,
    });

    const publishMutation = usePublishCourse();
    const unpublishMutation = useUnpublishCourse();
    const archiveMutation = useArchiveCourse();
    const unarchiveMutation = useUnarchiveCourse();
    const deleteMutation = useDeleteCourse();

    const courses = data?.items ?? [];
    const totalPages = data?.totalPages ?? 0;
    const currentPage = Math.floor(skip / pageSize) + 1;

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
                  title: t('common:actions.delete'),
                  description: t('confirmDelete', { title: pending.course.title }),
                  confirmLabel: t('common:actions.delete'),
                  variant: 'destructive' as const,
              }
        : null;

    let tableBody: ReactNode;
    if (isLoading) {
        tableBody = SKELETON_ROWS.map((row) => (
            <TableRow key={row}>
                <TableCell className="px-5 py-4">
                    <Skeleton className="h-10 w-full" />
                </TableCell>
                <TableCell className="px-5 py-4">
                    <Skeleton className="h-6 w-20" />
                </TableCell>
                <TableCell className="px-5 py-4">
                    <Skeleton className="h-6 w-10" />
                </TableCell>
                <TableCell className="px-5 py-4">
                    <Skeleton className="ml-auto h-8 w-32" />
                </TableCell>
            </TableRow>
        ));
    } else if (courses.length === 0) {
        tableBody = (
            <TableRow>
                <TableCell colSpan={4} className="py-16 text-center">
                    <p className="text-sm text-muted-foreground">
                        {debouncedSearch ? t('myCoursesEmpty') : t('myCoursesEmptyDefault')}
                    </p>
                    {!debouncedSearch && (
                        <TextLink
                            to={APP_ROUTES.instructor.newCourse}
                            className="mt-3 inline-block text-sm"
                        >
                            {t('dashboardEmptyCta')}
                        </TextLink>
                    )}
                </TableCell>
            </TableRow>
        );
    } else {
        tableBody = courses.map((course) => (
            <InstructorCourseRow
                key={course.id}
                course={course}
                onArchive={(c) => setPending({ type: 'archive', course: c })}
                onDelete={(c) => setPending({ type: 'delete', course: c })}
                publishMutation={publishMutation}
                unpublishMutation={unpublishMutation}
                unarchiveMutation={unarchiveMutation}
            />
        ));
    }

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
                <Button asChild>
                    <Link to={APP_ROUTES.instructor.newCourse}>{t('btnNewCourse')}</Link>
                </Button>
            </div>

            {/* Search */}
            <div className="mb-4">
                <SearchInput
                    placeholder={t('myCoursesSearch')}
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    onClear={() => setSearch('')}
                    containerClassName="max-w-sm"
                />
            </div>

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow className="bg-secondary/50 hover:bg-secondary/50">
                            <TableHead className="w-1/2">{t('common:general.course')}</TableHead>
                            <TableHead>{t('common:status.status')}</TableHead>
                            <TableHead>{t('colStudents')}</TableHead>
                            <TableHead className="text-right">{t('colActions')}</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>{tableBody}</TableBody>
                </Table>

                {/* Footer Controls */}
                <div className="flex items-center justify-between border-t border-border px-5 py-3">
                    <PageSizeSelect
                        value={pageSize}
                        onChange={(size) => {
                            setPageSize(size);
                            setSkip(0);
                        }}
                        options={[10, 20, 50, 100]}
                        label={t('rowsPerPage', { defaultValue: 'Rows per page:' })}
                    />

                    <Pagination
                        page={currentPage}
                        totalPages={totalPages}
                        onChange={(p) => setSkip((p - 1) * pageSize)}
                        prevLabel={t('common:actions.previous')}
                        nextLabel={t('common:actions.next')}
                    />
                </div>
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
