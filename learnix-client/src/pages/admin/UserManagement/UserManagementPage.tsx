import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { Pagination } from '@/components/common/Pagination';
import { PAGINATION } from '@/const/ui.constants';
import { useAuthStore } from '@/store/auth.store';
import type { AdminUserDto } from '@/types/admin.types';
import { ChangeRoleDialog } from './ChangeRoleDialog';
import { UserTableRow } from './components/UserTableRow';

const PAGE_SIZE = PAGINATION.DEFAULT;

export type PendingAction =
    | { type: 'ban'; user: AdminUserDto }
    | { type: 'unban'; user: AdminUserDto }
    | { type: 'delete'; user: AdminUserDto }
    | { type: 'recover'; user: AdminUserDto };

export default function UserManagementPage() {
    const { t } = useTranslation('admin');
    const currentUser = useAuthStore((s) => s.user);
    const qc = useQueryClient();
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [skip, setSkip] = useState(0);
    const [roleDialogUserId, setRoleDialogUserId] = useState<string | null>(null);
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
                                <UserTableRow
                                    key={u.id}
                                    user={u}
                                    currentUserId={currentUser?.id}
                                    onSetRole={setRoleDialogUserId}
                                    onSetPending={setPending}
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
                    prevLabel={t('prev')}
                    nextLabel={t('next')}
                    className="border-t border-border px-5 py-3"
                />
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
