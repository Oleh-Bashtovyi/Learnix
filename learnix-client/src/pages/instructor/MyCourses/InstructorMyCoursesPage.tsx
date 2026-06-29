import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ChevronDown } from 'lucide-react';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { Pagination } from '@/components/common/ui/Pagination';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
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
                <Button asChild>
                    <Link to={APP_ROUTES.instructor.newCourse}>{t('btnNewCourse')}</Link>
                </Button>
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
                <Table>
                    <TableHeader>
                        <TableRow className="bg-secondary/50 hover:bg-secondary/50">
                            <TableHead className="w-1/2">{t('colCourse')}</TableHead>
                            <TableHead>{t('colStatus')}</TableHead>
                            <TableHead>{t('colStudents')}</TableHead>
                            <TableHead className="text-right">{t('colActions')}</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 3 }).map((_, i) => (
                                <TableRow key={i}>
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
                            ))
                        ) : courses.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={4} className="py-16 text-center">
                                    <p className="text-sm text-muted-foreground">
                                        {debouncedSearch
                                            ? t('myCoursesEmpty')
                                            : t('myCoursesEmptyDefault')}
                                    </p>
                                    {!debouncedSearch && (
                                        <Link
                                            to={APP_ROUTES.instructor.newCourse}
                                            className="mt-3 inline-block text-sm text-primary hover:underline"
                                        >
                                            {t('dashboardEmptyCta')}
                                        </Link>
                                    )}
                                </TableCell>
                            </TableRow>
                        ) : (
                            courses.map((course) => (
                                <InstructorCourseRow
                                    key={course.id}
                                    course={course}
                                    onArchive={(c) => setPending({ type: 'archive', course: c })}
                                    onDelete={(c) => setPending({ type: 'delete', course: c })}
                                    publishMutation={publishMutation}
                                    unpublishMutation={unpublishMutation}
                                    unarchiveMutation={unarchiveMutation}
                                />
                            ))
                        )}
                    </TableBody>
                </Table>

                {/* Footer Controls */}
                <div className="flex items-center justify-between border-t border-border px-5 py-3">
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <span>{t('rowsPerPage', { defaultValue: 'Rows per page:' })}</span>
                        <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                                <button className="flex items-center gap-1 rounded-md border border-border px-2 py-1 hover:bg-secondary">
                                    {pageSize} <ChevronDown className="size-4 opacity-50" />
                                </button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="start">
                                {[10, 20, 50, 100].map((size) => (
                                    <DropdownMenuItem
                                        key={size}
                                        onClick={() => {
                                            setPageSize(size);
                                            setSkip(0);
                                        }}
                                        className={pageSize === size ? 'bg-secondary' : ''}
                                    >
                                        {size}
                                    </DropdownMenuItem>
                                ))}
                            </DropdownMenuContent>
                        </DropdownMenu>
                    </div>

                    <Pagination
                        page={currentPage}
                        totalPages={totalPages}
                        onChange={(p) => setSkip((p - 1) * pageSize)}
                        prevLabel={t('paginationPrev')}
                        nextLabel={t('paginationNext')}
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
