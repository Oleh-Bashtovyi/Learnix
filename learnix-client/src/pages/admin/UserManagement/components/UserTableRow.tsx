import { useTranslation } from 'react-i18next';
import { Ban, Key, RefreshCw, ShieldCheck, Trash2 } from 'lucide-react';
import type { AdminUserDto } from '@/types/admin.types';
import { cn } from '@/utils/cn';
import type { PendingAction } from '../UserManagementPage';

const ROLE_STYLES: Record<string, string> = {
    Student: 'bg-primary/10 text-primary',
    Instructor: 'bg-accent/10 text-accent',
    Admin: 'bg-destructive/10 text-destructive',
};

function userInitials(u: AdminUserDto) {
    return `${u.firstName[0] ?? ''}${u.lastName[0] ?? ''}`.toUpperCase();
}

interface UserTableRowProps {
    user: AdminUserDto;
    currentUserId?: string;
    onSetRole: (userId: string) => void;
    onSetPending: (action: PendingAction) => void;
}

export function UserTableRow({
    user: u,
    currentUserId,
    onSetRole,
    onSetPending,
}: UserTableRowProps) {
    const { t } = useTranslation('admin');

    return (
        <tr className={cn('hover:bg-secondary/30', u.isDeleted && 'opacity-50')}>
            {/* User */}
            <td className="px-5 py-3">
                <div className="flex items-center gap-3">
                    <div className="relative grid size-9 shrink-0 place-items-center overflow-hidden rounded-full bg-primary/20 text-xs font-semibold text-primary">
                        {u.avatarUrl ? (
                            <img src={u.avatarUrl} alt="" className="size-full object-cover" />
                        ) : (
                            userInitials(u)
                        )}
                    </div>
                    <div>
                        <p className="font-medium text-foreground">
                            {u.firstName} {u.lastName}
                        </p>
                        <p className="text-xs text-muted-foreground">{u.email}</p>
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
                                ROLE_STYLES[r] ?? 'bg-muted text-muted-foreground',
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
                        onClick={() => onSetRole(u.id)}
                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-primary"
                        title={t('btnChangeRole')}
                    >
                        <Key size={14} />
                    </button>
                    {u.id !== currentUserId && (
                        <>
                            {!u.isDeleted &&
                                (u.isBanned ? (
                                    <button
                                        onClick={() =>
                                            onSetPending({
                                                type: 'unban',
                                                user: u,
                                            })
                                        }
                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                        title={t('btnUnban')}
                                    >
                                        <ShieldCheck size={14} />
                                    </button>
                                ) : (
                                    <button
                                        onClick={() =>
                                            onSetPending({
                                                type: 'ban',
                                                user: u,
                                            })
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
                                        onSetPending({
                                            type: 'recover',
                                            user: u,
                                        })
                                    }
                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                                    title={t('btnRecover')}
                                >
                                    <RefreshCw size={14} />
                                </button>
                            ) : (
                                <button
                                    onClick={() =>
                                        onSetPending({
                                            type: 'delete',
                                            user: u,
                                        })
                                    }
                                    className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                                    title={t('btnDelete')}
                                >
                                    <Trash2 size={14} />
                                </button>
                            )}
                        </>
                    )}
                </div>
            </td>
        </tr>
    );
}
