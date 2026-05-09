import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { EyeOff, Trash2, RefreshCw } from 'lucide-react';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { ADMIN } from '@/const/localization/admin';
import { cn } from '@/utils/cn';
import type { ManageCourseCardDto, CourseStatus } from '@/types/course.types';

const PAGE_SIZE = 20;

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

const STATUS_LABELS: Record<CourseStatus, string> = {
    Published: ADMIN.COURSE_STATUS_PUBLISHED,
    Draft: ADMIN.COURSE_STATUS_DRAFT,
    Archived: ADMIN.COURSE_STATUS_ARCHIVED,
};

export default function CourseModerationPage() {
    const qc = useQueryClient();
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [includeDeleted, setIncludeDeleted] = useState(false);
    const [skip, setSkip] = useState(0);

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
        take: PAGE_SIZE,
    };

    const { data, isLoading } = useQuery({
        queryKey: queryKeys.admin.courses(filters as Record<string, unknown>),
        queryFn: () => adminApi.getCourses(filters),
    });

    function invalidateCourses() {
        qc.invalidateQueries({ queryKey: queryKeys.admin.coursesList() });
    }

    const unpublishMutation = useMutation({
        mutationFn: (id: string) => adminApi.unpublishCourse(id),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_UNPUBLISHED);
            invalidateCourses();
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => adminApi.deleteCourse(id),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_COURSE_DELETED);
            invalidateCourses();
        },
    });

    const recoverMutation = useMutation({
        mutationFn: (id: string) => adminApi.recoverCourse(id),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_COURSE_RECOVERED);
            invalidateCourses();
        },
    });

    const courses = data?.items ?? [];
    const totalPages = data?.totalPages ?? 0;
    const currentPage = Math.floor(skip / PAGE_SIZE) + 1;

    function handleUnpublish(c: ManageCourseCardDto) {
        if (confirm(ADMIN.CONFIRM_UNPUBLISH(c.title))) unpublishMutation.mutate(c.id);
    }

    function handleDelete(c: ManageCourseCardDto) {
        if (confirm(ADMIN.CONFIRM_DELETE_COURSE(c.title))) deleteMutation.mutate(c.id);
    }

    function handleRecover(c: ManageCourseCardDto) {
        if (confirm(ADMIN.CONFIRM_RECOVER_COURSE(c.title))) recoverMutation.mutate(c.id);
    }

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8">
                <h1 className="font-heading text-3xl font-bold text-foreground">
                    {ADMIN.COURSES_TITLE}
                </h1>
                <p className="mt-1 text-muted-foreground">{ADMIN.COURSES_SUBTITLE}</p>
            </div>

            {/* Toolbar */}
            <div className="mb-4 flex items-center gap-4">
                <input
                    type="text"
                    placeholder={ADMIN.COURSES_SEARCH}
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
                    {ADMIN.COURSES_SHOW_DELETED}
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
                        {ADMIN.EMPTY_COURSES}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_COURSE}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_COURSE_STATUS}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_ENROLLMENTS}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_PRICE}
                                </th>
                                <th className="px-5 py-3 text-right font-medium">
                                    {ADMIN.COL_ACTIONS}
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
                                                        {ADMIN.COURSE_STATUS_DELETED}
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
                                        {c.isFree ? ADMIN.COURSE_FREE : `$${c.price.toFixed(2)}`}
                                    </td>

                                    {/* Actions */}
                                    <td className="px-5 py-3">
                                        <div className="flex items-center justify-end gap-1">
                                            {!c.isDeleted && c.status === 'Published' && (
                                                <button
                                                    onClick={() => handleUnpublish(c)}
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                    title={ADMIN.BTN_UNPUBLISH}
                                                >
                                                    <EyeOff size={14} />
                                                </button>
                                            )}
                                            {c.isDeleted ? (
                                                <button
                                                    onClick={() => handleRecover(c)}
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={ADMIN.BTN_RECOVER_COURSE}
                                                >
                                                    <RefreshCw size={14} />
                                                </button>
                                            ) : (
                                                <button
                                                    onClick={() => handleDelete(c)}
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                                                    title={ADMIN.BTN_DELETE_COURSE}
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
            </div>
        </div>
    );
}
