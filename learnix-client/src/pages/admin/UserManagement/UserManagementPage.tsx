import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Key, Ban, Trash2, RefreshCw, ShieldCheck } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { ChangeRoleDialog } from './ChangeRoleDialog';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { PAGINATION } from '@/const/ui.constants';
import { cn } from '@/utils/cn';
import type { AdminUserDto } from '@/types/admin.types';

const PAGE_SIZE = PAGINATION.DEFAULT;

const ROLE_STYLES: Record<string, string> = {
    Student: 'bg-primary/10 text-primary',
    Instructor: 'bg-accent/10 text-accent',
    Admin: 'bg-destructive/10 text-destructive',
};

type PendingAction =
    | { type: 'ban'; user: AdminUserDto }
    | { type: 'unban'; user: AdminUserDto }
    | { type: 'delete'; user: AdminUserDto }
    | { type: 'recover'; user: AdminUserDto };

function userInitials(u: AdminUserDto) {
    return `${u.firstName[0] ?? ''}${u.lastName[0] ?? ''}`.toUpperCase();
}

export default function UserManagementPage() {
    const { t } = useTranslation('admin');
    const qc = useQueryClient();
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [skip, setSkip] = useState(0);
    const [roleDialogUser, setRoleDialogUser] = useState<AdminUserDto | null>(null);
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
        skip,
        take: PAGE_SIZE,
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
    const currentPage = Math.floor(skip / PAGE_SIZE) + 1;

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

            {/* Search */}
            <div className="mb-4">
                <input
                    type="text"
                    placeholder={t('usersSearch')}
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    className="w-full max-w-sm rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                {isLoading ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        Loading...
                    </div>
                ) : users.length === 0 ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        {t('emptyUsers')}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">{t('colUser')}</th>
                                <th className="px-5 py-3 text-left font-medium">{t('colRoles')}</th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colStatus')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colJoined')}
                                </th>
                                <th className="px-5 py-3 text-right font-medium">
                                    {t('colActions')}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {users.map((u) => (
                                <tr
                                    key={u.id}
                                    className={cn(
                                        'hover:bg-secondary/30',
                                        u.isDeleted && 'opacity-50',
                                    )}
                                >
                                    {/* User */}
                                    <td className="px-5 py-3">
                                        <div className="flex items-center gap-3">
                                            <div className="relative grid h-9 w-9 shrink-0 place-items-center overflow-hidden rounded-full bg-primary/20 text-xs font-semibold text-primary">
                                                {u.avatarUrl ? (
                                                    <img
                                                        src={u.avatarUrl}
                                                        alt=""
                                                        className="h-full w-full object-cover"
                                                    />
                                                ) : (
                                                    userInitials(u)
                                                )}
                                            </div>
                                            <div>
                                                <p className="font-medium text-foreground">
                                                    {u.firstName} {u.lastName}
                                                </p>
                                                <p className="text-xs text-muted-foreground">
                                                    {u.email}
                                                </p>
                                            </div>
                                        </div>
                                    </td>

                                    {/* Roles */}
                                    <td className="px-5 py-3">
                                        <div className="flex flex-wrap gap-1">
                                            {u.roles.map((r) => (
                                                <span
                                                    key={r}
                                                    className={cn(
                                                        'rounded px-2 py-0.5 text-xs font-medium',
                                                        ROLE_STYLES[r] ??
                                                            'bg-muted text-muted-foreground',
                                                    )}
                                                >
                                                    {r}
                                                </span>
                                            ))}
                                        </div>
                                    </td>

                                    {/* Status */}
                                    <td className="px-5 py-3">
                                        {u.isDeleted ? (
                                            <span className="rounded bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive">
                                                {t('statusDeleted')}
                                            </span>
                                        ) : u.isBanned ? (
                                            <span className="rounded bg-warning/20 px-2 py-0.5 text-xs font-medium text-warning">
                                                {t('statusBanned')}
                                            </span>
                                        ) : (
                                            <span className="rounded bg-success/20 px-2 py-0.5 text-xs font-medium text-success">
                                                {t('statusActive')}
                                            </span>
                                        )}
                                    </td>

                                    {/* Joined */}
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {new Date(u.createdAt).toLocaleDateString()}
                                    </td>

                                    {/* Actions */}
                                    <td className="px-5 py-3">
                                        <div className="flex items-center justify-end gap-1">
                                            <button
                                                onClick={() => setRoleDialogUser(u)}
                                                className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-primary"
                                                title={t('btnChangeRole')}
                                            >
                                                <Key size={14} />
                                            </button>
                                            {!u.isDeleted &&
                                                (u.isBanned ? (
                                                    <button
                                                        onClick={() =>
                                                            setPending({ type: 'unban', user: u })
                                                        }
                                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                        title={t('btnUnban')}
                                                    >
                                                        <ShieldCheck size={14} />
                                                    </button>
                                                ) : (
                                                    <button
                                                        onClick={() =>
                                                            setPending({ type: 'ban', user: u })
                                                        }
                                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                        title={t('btnBan')}
                                                    >
                                                        <Ban size={14} />
                                                    </button>
                                                ))}
                                            {u.isDeleted ? (
                                                <button
                                                    onClick={() =>
                                                        setPending({ type: 'recover', user: u })
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={t('btnRecover')}
                                                >
                                                    <RefreshCw size={14} />
                                                </button>
                                            ) : (
                                                <button
                                                    onClick={() =>
                                                        setPending({ type: 'delete', user: u })
                                                    }
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                                                    title={t('btnDelete')}
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

            {/* Role dialog */}
            {roleDialogUser && (
                <ChangeRoleDialog
                    user={roleDialogUser}
                    onClose={() => setRoleDialogUser(null)}
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
