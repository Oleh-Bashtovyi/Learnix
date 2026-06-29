import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ChevronDown } from 'lucide-react';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { Pagination } from '@/components/common/ui/Pagination';
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
import { useDebounce } from '@/hooks/shared/useDebounce';
import { useAuthStore } from '@/store/auth.store';
import type { AdminUserDto } from '@/types/admin.types';
import { ChangeRoleDialog } from './ChangeRoleDialog';
import { UserTableRow } from './components/UserTableRow';

const DEFAULT_PAGE_SIZE = PAGINATION.DEFAULT;
export type PendingAction =
    | { type: 'ban'; user: AdminUserDto }
    | { type: 'unban'; user: AdminUserDto }
    | { type: 'delete'; user: AdminUserDto }
    | { type: 'recover'; user: AdminUserDto };

export default function UserManagementPage() {
    const { t } = useTranslation('admin');
    const currentUser = useAuthStore((s) => s.user);
    const qc = useQueryClient();

    const [searchParams, setSearchParams] = useSearchParams();

    const searchParam = searchParams.get('q') ?? '';
    const skipParam = parseInt(searchParams.get('skip') ?? '0', 10) || 0;
    const sizeParam =
        parseInt(searchParams.get('size') ?? String(DEFAULT_PAGE_SIZE), 10) || DEFAULT_PAGE_SIZE;
    const includeDeletedParam = searchParams.get('includeDeleted') === 'true';

    const [search, setSearch] = useState(searchParam);
    const debouncedSearch = useDebounce(search, 400);

    const skip = skipParam;
    const pageSize = sizeParam;
    const includeDeleted = includeDeletedParam;

    const [roleDialogUserId, setRoleDialogUserId] = useState<string | null>(null);
    const [pending, setPending] = useState<PendingAction | null>(null);

    const setParam = useCallback(
        (updates: Record<string, string | null>) => {
            setSearchParams((prev) => {
                const next = new URLSearchParams(prev);
                Object.entries(updates).forEach(([k, v]) => {
                    if (v === null || v === '') next.delete(k);
                    else next.set(k, v);
                });
                return next;
            });
        },
        [setSearchParams],
    );

    const prevSearchRef = useRef(debouncedSearch);
    const isFirstMount = useRef(true);

    useEffect(() => {
        if (isFirstMount.current) {
            isFirstMount.current = false;
            return;
        }

        if (prevSearchRef.current !== debouncedSearch) {
            prevSearchRef.current = debouncedSearch;
            setParam({
                q: debouncedSearch || null,
                skip: null,
            });
        }
    }, [debouncedSearch, setParam]);

    const handleSetSkip = (newSkip: number) => {
        setParam({ skip: newSkip === 0 ? null : String(newSkip) });
    };

    const handleSetPageSize = (newSize: number) => {
        setParam({
            size: newSize === DEFAULT_PAGE_SIZE ? null : String(newSize),
            skip: null,
        });
    };

    /**
     * Related ADRs:
     * - ADR-FRONT-API-008: Pagination Strategies (Offset-based)
     */
    const filters = {
        search: debouncedSearch || undefined,
        skip,
        take: pageSize,
        includeDeleted,
    };

    const { data, isLoading } = useQuery({
        queryKey: queryKeys.admin.users(filters as Record<string, unknown>),
        queryFn: () => adminApi.getUsers(filters),
    });

    function invalidateUsers() {
        qc.invalidateQueries({ queryKey: queryKeys.admin.usersList() });
    }

    const banMutation = useMutation({
        mutationFn: (id: string) => adminApi.banUser(id),
        onSuccess: () => {
            toast.success(t('toastBanned'));
            invalidateUsers();
            setPending(null);
        },
    });

    const unbanMutation = useMutation({
        mutationFn: (id: string) => adminApi.unbanUser(id),
        onSuccess: () => {
            toast.success(t('toastUnbanned'));
            invalidateUsers();
            setPending(null);
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => adminApi.deleteUser(id),
        onSuccess: () => {
            toast.success(t('toastUserDeleted'));
            invalidateUsers();
            setPending(null);
        },
    });

    const recoverMutation = useMutation({
        mutationFn: (id: string) => adminApi.recoverUser(id),
        onSuccess: () => {
            toast.success(t('toastUserRecovered'));
            invalidateUsers();
            setPending(null);
        },
    });

    const users = data?.items ?? [];
    const totalPages = data?.totalPages ?? 0;
    const currentPage = Math.floor(skip / pageSize) + 1;

    const roleDialogUser = roleDialogUserId ? users.find((u) => u.id === roleDialogUserId) : null;

    const isAnyPending =
        banMutation.isPending ||
        unbanMutation.isPending ||
        deleteMutation.isPending ||
        recoverMutation.isPending;

    function handleConfirm() {
        if (!pending) return;
        if (pending.type === 'ban') banMutation.mutate(pending.user.id);
        else if (pending.type === 'unban') unbanMutation.mutate(pending.user.id);
        else if (pending.type === 'delete') deleteMutation.mutate(pending.user.id);
        else if (pending.type === 'recover') recoverMutation.mutate(pending.user.id);
    }

    function dialogProps(): {
        title: string;
        description: string;
        confirmLabel: string;
        variant: 'destructive' | 'warning' | 'default';
    } | null {
        if (!pending) return null;
        const name = `${pending.user.firstName} ${pending.user.lastName}`;
        if (pending.type === 'ban')
            return {
                title: t('btnBan'),
                description: t('confirmBan', { name }),
                confirmLabel: t('btnBan'),
                variant: 'warning',
            };
        if (pending.type === 'unban')
            return {
                title: t('btnUnban'),
                description: t('confirmUnban', { name }),
                confirmLabel: t('btnUnban'),
                variant: 'default',
            };
        if (pending.type === 'delete')
            return {
                title: t('btnDelete'),
                description: t('confirmDelete', { name }),
                confirmLabel: t('btnDelete'),
                variant: 'destructive',
            };
        if (pending.type === 'recover')
            return {
                title: t('btnRecover'),
                description: t('confirmRecover', { name }),
                confirmLabel: t('btnRecover'),
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
                    {t('usersTitle')}
                </h1>
                <p className="mt-1 text-muted-foreground">{t('usersSubtitle')}</p>
            </div>

            {/* Toolbar */}
            <div className="mb-4 flex items-center gap-4">
                <input
                    type="text"
                    placeholder={t('usersSearch')}
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    className="w-full max-w-sm rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                <label className="flex cursor-pointer items-center gap-2 text-sm text-foreground">
                    <input
                        type="checkbox"
                        checked={includeDeleted}
                        onChange={(e) => {
                            setParam({
                                includeDeleted: e.target.checked ? 'true' : null,
                                skip: null,
                            });
                        }}
                        className="accent-primary"
                    />
                    {t('usersShowDeleted')}
                </label>
            </div>

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow className="bg-secondary/50 text-xs uppercase tracking-wider hover:bg-secondary/50">
                            <TableHead>{t('colUser')}</TableHead>
                            <TableHead>{t('colRoles')}</TableHead>
                            <TableHead>{t('colStatus')}</TableHead>
                            <TableHead>{t('colJoined')}</TableHead>
                            <TableHead className="text-right">{t('colActions')}</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 3 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell>
                                        <Skeleton className="h-10 w-full" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton className="h-6 w-24" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton className="h-6 w-16" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton className="h-6 w-20" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton className="ml-auto h-8 w-24" />
                                    </TableCell>
                                </TableRow>
                            ))
                        ) : users.length === 0 ? (
                            <TableRow>
                                <TableCell
                                    colSpan={5}
                                    className="py-16 text-center text-muted-foreground"
                                >
                                    {t('emptyUsers')}
                                </TableCell>
                            </TableRow>
                        ) : (
                            users.map((u) => (
                                <UserTableRow
                                    key={u.id}
                                    user={u}
                                    currentUserId={currentUser?.id}
                                    onSetRole={setRoleDialogUserId}
                                    onSetPending={setPending}
                                />
                            ))
                        )}
                    </TableBody>
                </Table>

                {/* Footer Controls */}
                <div className="flex items-center justify-between border-t border-border px-5 py-3">
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <span>{t('rowsPerPage')}</span>
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
                                        onClick={() => handleSetPageSize(size)}
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
                        onChange={(p) => handleSetSkip((p - 1) * pageSize)}
                        prevLabel={t('prev')}
                        nextLabel={t('next')}
                    />
                </div>
            </div>

            {/* Role dialog */}
            {roleDialogUser && (
                <ChangeRoleDialog
                    user={roleDialogUser}
                    onClose={() => setRoleDialogUserId(null)}
                    onRolesChanged={invalidateUsers}
                />
            )}

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
