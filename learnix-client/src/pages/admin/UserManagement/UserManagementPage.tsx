import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Key, Ban, Trash2, RefreshCw, ShieldCheck } from 'lucide-react';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { ChangeRoleDialog } from './ChangeRoleDialog';
import { ADMIN } from '@/const/localization/admin';
import { cn } from '@/utils/cn';
import type { AdminUserDto } from '@/types/admin.types';

const PAGE_SIZE = 20;

const ROLE_STYLES: Record<string, string> = {
    Student: 'bg-primary/10 text-primary',
    Instructor: 'bg-accent/10 text-accent',
    Admin: 'bg-destructive/10 text-destructive',
};

function userInitials(u: AdminUserDto) {
    return `${u.firstName[0] ?? ''}${u.lastName[0] ?? ''}`.toUpperCase();
}

export default function UserManagementPage() {
    const qc = useQueryClient();
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [skip, setSkip] = useState(0);
    const [roleDialogUser, setRoleDialogUser] = useState<AdminUserDto | null>(null);

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
            toast.success(ADMIN.TOAST_BANNED);
            invalidateUsers();
        },
    });

    const unbanMutation = useMutation({
        mutationFn: (id: string) => adminApi.unbanUser(id),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_UNBANNED);
            invalidateUsers();
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => adminApi.deleteUser(id),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_USER_DELETED);
            invalidateUsers();
        },
    });

    const recoverMutation = useMutation({
        mutationFn: (id: string) => adminApi.recoverUser(id),
        onSuccess: () => {
            toast.success(ADMIN.TOAST_USER_RECOVERED);
            invalidateUsers();
        },
    });

    const users = data?.items ?? [];
    const totalPages = data?.totalPages ?? 0;
    const currentPage = Math.floor(skip / PAGE_SIZE) + 1;

    function handleBan(u: AdminUserDto) {
        if (confirm(ADMIN.CONFIRM_BAN(`${u.firstName} ${u.lastName}`))) {
            banMutation.mutate(u.id);
        }
    }

    function handleUnban(u: AdminUserDto) {
        if (confirm(ADMIN.CONFIRM_UNBAN(`${u.firstName} ${u.lastName}`))) {
            unbanMutation.mutate(u.id);
        }
    }

    function handleDelete(u: AdminUserDto) {
        if (confirm(ADMIN.CONFIRM_DELETE(`${u.firstName} ${u.lastName}`))) {
            deleteMutation.mutate(u.id);
        }
    }

    function handleRecover(u: AdminUserDto) {
        if (confirm(ADMIN.CONFIRM_RECOVER(`${u.firstName} ${u.lastName}`))) {
            recoverMutation.mutate(u.id);
        }
    }

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8">
                <h1 className="font-heading text-3xl font-bold text-foreground">
                    {ADMIN.USERS_TITLE}
                </h1>
                <p className="mt-1 text-muted-foreground">{ADMIN.USERS_SUBTITLE}</p>
            </div>

            {/* Search */}
            <div className="mb-4">
                <input
                    type="text"
                    placeholder={ADMIN.USERS_SEARCH}
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
                        {ADMIN.EMPTY_USERS}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_USER}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_ROLES}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_STATUS}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_JOINED}
                                </th>
                                <th className="px-5 py-3 text-right font-medium">
                                    {ADMIN.COL_ACTIONS}
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
                                                {u.avatarBlobPath ? (
                                                    <img
                                                        src={u.avatarBlobPath}
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
                                                {ADMIN.STATUS_DELETED}
                                            </span>
                                        ) : u.isBanned ? (
                                            <span className="rounded bg-warning/20 px-2 py-0.5 text-xs font-medium text-warning">
                                                {ADMIN.STATUS_BANNED}
                                            </span>
                                        ) : (
                                            <span className="rounded bg-success/20 px-2 py-0.5 text-xs font-medium text-success">
                                                {ADMIN.STATUS_ACTIVE}
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
                                                title={ADMIN.BTN_CHANGE_ROLE}
                                            >
                                                <Key size={14} />
                                            </button>
                                            {!u.isDeleted &&
                                                (u.isBanned ? (
                                                    <button
                                                        onClick={() => handleUnban(u)}
                                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                        title={ADMIN.BTN_UNBAN}
                                                    >
                                                        <ShieldCheck size={14} />
                                                    </button>
                                                ) : (
                                                    <button
                                                        onClick={() => handleBan(u)}
                                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                                                        title={ADMIN.BTN_BAN}
                                                    >
                                                        <Ban size={14} />
                                                    </button>
                                                ))}
                                            {u.isDeleted ? (
                                                <button
                                                    onClick={() => handleRecover(u)}
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                                    title={ADMIN.BTN_RECOVER}
                                                >
                                                    <RefreshCw size={14} />
                                                </button>
                                            ) : (
                                                <button
                                                    onClick={() => handleDelete(u)}
                                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                                                    title={ADMIN.BTN_DELETE}
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

            {/* Role dialog */}
            {roleDialogUser && (
                <ChangeRoleDialog
                    user={roleDialogUser}
                    onClose={() => setRoleDialogUser(null)}
                    onRolesChanged={invalidateUsers}
                />
            )}
        </div>
    );
}
