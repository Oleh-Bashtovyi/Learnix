import { type ReactNode, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { FormCheckbox } from '@/components/common/form/FormCheckbox';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { PageSizeSelect } from '@/components/common/ui/PageSizeSelect';
import { Pagination } from '@/components/common/ui/Pagination';
import { SearchInput } from '@/components/common/ui/SearchInput';
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
import type { ManageCourseCardDto } from '@/types/course.types';
import { CourseModerationTableRow } from './components/CourseModerationTableRow';

const DEFAULT_PAGE_SIZE = PAGINATION.DEFAULT;
export type PendingAction =
    | { type: 'publish'; course: ManageCourseCardDto }
    | { type: 'unpublish'; course: ManageCourseCardDto }
    | { type: 'delete'; course: ManageCourseCardDto }
    | { type: 'recover'; course: ManageCourseCardDto };

/** Placeholder rows shown while the page loads; the value is the key, not an index. */
const SKELETON_ROWS = ['s1', 's2', 's3'];

export default function CourseModerationPage() {
    const { t } = useTranslation('admin');
    const qc = useQueryClient();
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [includeDeleted, setIncludeDeleted] = useState(false);
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

    const filters = {
        search: debouncedSearch || undefined,
        includeDeleted,
        skip,
        take: pageSize,
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
    const currentPage = Math.floor(skip / pageSize) + 1;

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
                title: t('common:actions.publish'),
                description: t('confirmPublish', { title: pending.course.title }),
                confirmLabel: t('common:actions.publish'),
                variant: 'default',
            };
        if (pending.type === 'unpublish')
            return {
                title: t('common:actions.unpublish'),
                description: t('confirmUnpublish', { title: pending.course.title }),
                confirmLabel: t('common:actions.unpublish'),
                variant: 'warning',
            };
        if (pending.type === 'delete')
            return {
                title: t('common:actions.delete'),
                description: t('confirmDeleteCourse', { title: pending.course.title }),
                confirmLabel: t('common:actions.delete'),
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

    let tableBody: ReactNode;
    if (isLoading) {
        tableBody = SKELETON_ROWS.map((row) => (
            <TableRow key={row}>
                <TableCell>
                    <Skeleton className="h-10 w-full" />
                </TableCell>
                <TableCell>
                    <Skeleton className="h-6 w-20" />
                </TableCell>
                <TableCell>
                    <Skeleton className="h-6 w-16" />
                </TableCell>
                <TableCell>
                    <Skeleton className="h-6 w-16" />
                </TableCell>
                <TableCell>
                    <Skeleton className="ml-auto h-8 w-24" />
                </TableCell>
            </TableRow>
        ));
    } else if (courses.length === 0) {
        tableBody = (
            <TableRow>
                <TableCell colSpan={5} className="py-16 text-center text-muted-foreground">
                    {t('emptyCourses')}
                </TableCell>
            </TableRow>
        );
    } else {
        tableBody = courses.map((c) => (
            <CourseModerationTableRow key={c.id} course={c} onSetPending={setPending} />
        ));
    }

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
                <SearchInput
                    placeholder={t('coursesSearch')}
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    onClear={() => setSearch('')}
                    containerClassName="w-full max-w-sm"
                />
                <FormCheckbox
                    label={t('coursesShowDeleted')}
                    checked={includeDeleted}
                    onChange={(e) => {
                        setIncludeDeleted(e.target.checked);
                        setSkip(0);
                    }}
                />
            </div>

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow className="bg-secondary/50 text-xs uppercase tracking-wider hover:bg-secondary/50">
                            <TableHead>{t('common:general.course')}</TableHead>
                            <TableHead>{t('common:status.status')}</TableHead>
                            <TableHead>{t('colEnrollments')}</TableHead>
                            <TableHead>{t('common:general.price')}</TableHead>
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
                        label={t('rowsPerPage')}
                    />

                    <Pagination
                        page={currentPage}
                        totalPages={totalPages}
                        onChange={(p) => setSkip((p - 1) * pageSize)}
                    />
                </div>
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
