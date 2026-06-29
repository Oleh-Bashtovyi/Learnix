import { useTranslation } from 'react-i18next';
import { Ban, Key, RefreshCw, ShieldCheck, Trash2 } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { TableCell, TableRow } from '@/components/ui/table';
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
        <TableRow className={cn('hover:bg-secondary/30', u.isDeleted && 'opacity-50')}>
            {/* User */}
            <TableCell className="px-5 py-3">
                <div className="flex items-center gap-3">
                    <Avatar className="size-9 bg-primary/20 text-primary">
                        <AvatarImage src={u.avatarUrl || ''} />
                        <AvatarFallback className="text-xs font-semibold">
                            {userInitials(u)}
                        </AvatarFallback>
                    </Avatar>
                    <div>
                        <p className="font-medium text-foreground">
                            {u.firstName} {u.lastName}
                        </p>
                        <p className="text-xs text-muted-foreground">{u.email}</p>
                    </div>
                </div>
            </TableCell>

            {/* Roles */}
            <TableCell className="px-5 py-3">
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
            </TableCell>

            {/* Status */}
            <TableCell className="px-5 py-3">
                {u.isDeleted ? (
                    <Badge
                        variant="destructive"
                        className="bg-destructive/10 text-destructive hover:bg-destructive/10"
                    >
                        {t('statusDeleted')}
                    </Badge>
                ) : u.isBanned ? (
                    <Badge
                        variant="outline"
                        className="border-transparent bg-warning/20 text-warning hover:bg-warning/20"
                    >
                        {t('statusBanned')}
                    </Badge>
                ) : (
                    <Badge
                        variant="outline"
                        className="border-transparent bg-success/20 text-success hover:bg-success/20"
                    >
                        {t('statusActive')}
                    </Badge>
                )}
            </TableCell>

            {/* Joined */}
            <TableCell className="px-5 py-3 text-muted-foreground">
                {new Date(u.createdAt).toLocaleDateString()}
            </TableCell>

            {/* Actions */}
            <TableCell className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onSetRole(u.id)}
                        className="size-8 text-muted-foreground hover:bg-primary/10 hover:text-primary"
                        title={t('btnChangeRole')}
                    >
                        <Key size={14} />
                    </Button>
                    {u.id !== currentUserId && (
                        <>
                            {!u.isDeleted &&
                                (u.isBanned ? (
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        onClick={() =>
                                            onSetPending({
                                                type: 'unban',
                                                user: u,
                                            })
                                        }
                                        className="size-8 text-muted-foreground hover:bg-success/10 hover:text-success"
                                        title={t('btnUnban')}
                                    >
                                        <ShieldCheck size={14} />
                                    </Button>
                                ) : (
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        onClick={() =>
                                            onSetPending({
                                                type: 'ban',
                                                user: u,
                                            })
                                        }
                                        className="size-8 text-muted-foreground hover:bg-warning/10 hover:text-warning"
                                        title={t('btnBan')}
                                    >
                                        <Ban size={14} />
                                    </Button>
                                ))}
                            {u.isDeleted ? (
                                <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() =>
                                        onSetPending({
                                            type: 'recover',
                                            user: u,
                                        })
                                    }
                                    className="size-8 text-muted-foreground hover:bg-success/10 hover:text-success"
                                    title={t('btnRecover')}
                                >
                                    <RefreshCw size={14} />
                                </Button>
                            ) : (
                                <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() =>
                                        onSetPending({
                                            type: 'delete',
                                            user: u,
                                        })
                                    }
                                    className="size-8 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                                    title={t('btnDelete')}
                                >
                                    <Trash2 size={14} />
                                </Button>
                            )}
                        </>
                    )}
                </div>
            </TableCell>
        </TableRow>
    );
}
