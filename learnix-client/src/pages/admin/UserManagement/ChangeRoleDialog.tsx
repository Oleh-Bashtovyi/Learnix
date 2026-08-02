import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import { X } from 'lucide-react';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { UserRole } from '@/enums/user.enums';
import { useAuthStore } from '@/store/auth.store';
import type { AdminUserDto } from '@/types/admin.types';
import { cn } from '@/utils/cn';
import { refreshSession } from '@/utils/refreshSession';

const ROLE_STYLES: Record<string, string> = {
    Student: 'bg-primary/10 text-primary',
    Instructor: 'bg-accent/10 text-accent-strong',
    Admin: 'bg-destructive/10 text-destructive',
};

interface Props {
    user: AdminUserDto;
    onClose: () => void;
    onRolesChanged: () => void;
}

export function ChangeRoleDialog({ user, onClose, onRolesChanged }: Props) {
    const { t } = useTranslation('admin');
    const currentUser = useAuthStore((s) => s.user);

    const [selectedRole, setSelectedRole] = useState<string>(UserRole.Instructor);

    // Roles are baked into the JWT, and nothing revokes it when an admin changes their own roles
    // (ADR-BACK-NOTIF-002) — refreshSession() re-issues the token so the change takes effect immediately.
    const refreshSelfIfNeeded = async () => {
        if (user.id !== currentUser?.id) return;
        try {
            await refreshSession();
        } catch (e) {
            console.error('Failed to refresh token after self-role change', e);
        }
    };

    const assignMutation = useMutation({
        mutationFn: (role: string) => adminApi.assignRole(user.id, role),
        onSuccess: async (_, role) => {
            toast.success(t('toastRoleAssigned', { role }));
            onRolesChanged();
            await refreshSelfIfNeeded();
        },
    });

    const removeMutation = useMutation({
        mutationFn: (role: string) => adminApi.removeRole(user.id, role),
        onSuccess: async (_, role) => {
            toast.success(t('toastRoleRemoved', { role }));
            onRolesChanged();
            await refreshSelfIfNeeded();
        },
        onError: (err: Error) => {
            toast.error(err?.message ?? t('toastRoleRemoveError'));
        },
    });

    const isLoading = assignMutation.isPending || removeMutation.isPending;

    return (
        <Dialog open onOpenChange={(open) => !open && onClose()}>
            <DialogContent className="sm:max-w-sm">
                <DialogHeader>
                    <DialogTitle>{t('roleDialogTitle')}</DialogTitle>
                </DialogHeader>

                <div className="space-y-4">
                    <div>
                        <p className="text-sm font-medium text-foreground">
                            {user.firstName} {user.lastName}
                        </p>
                        <p className="text-xs text-muted-foreground">{user.email}</p>
                    </div>

                    {/* Current roles */}
                    <div>
                        <p className="mb-2 text-xs uppercase tracking-wider text-muted-foreground">
                            {t('roleDialogCurrent')}
                        </p>
                        {user.roles.length === 0 ? (
                            <p className="text-sm text-muted-foreground">
                                {t('roleDialogNoRoles')}
                            </p>
                        ) : (
                            <div className="flex flex-wrap gap-2">
                                {user.roles.map((role) => (
                                    <span
                                        key={role}
                                        className={cn(
                                            'flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
                                            ROLE_STYLES[role] ?? 'bg-muted text-muted-foreground',
                                        )}
                                    >
                                        {role}
                                        {/* Student is the base role and cannot be removed. Admin cannot be removed from self */}
                                        {role !== UserRole.Student &&
                                            !(
                                                role === UserRole.Admin &&
                                                user.id === currentUser?.id
                                            ) && (
                                                <AsyncButton
                                                    type="button"
                                                    variant="ghost"
                                                    onClick={() => removeMutation.mutate(role)}
                                                    disabled={isLoading}
                                                    isLoading={
                                                        removeMutation.isPending &&
                                                        removeMutation.variables === role
                                                    }
                                                    // Fixed size-4 box so swapping the icon for the
                                                    // spinner never resizes the pill around it.
                                                    className="ml-0.5 size-4 rounded-full p-0 opacity-60 hover:bg-transparent hover:opacity-100"
                                                    title={t('roleDialogRemoveRole', { role })}
                                                >
                                                    <X />
                                                </AsyncButton>
                                            )}
                                    </span>
                                ))}
                            </div>
                        )}
                    </div>

                    {/* Assign role */}
                    <div>
                        <p className="mb-2 text-xs uppercase tracking-wider text-muted-foreground">
                            {t('roleDialogAddLabel')}
                        </p>
                        <div className="flex gap-2">
                            <Select value={selectedRole} onValueChange={setSelectedRole}>
                                <SelectTrigger variant="card" className="flex-1">
                                    <SelectValue placeholder={t('roleDialogSelectPlaceholder')} />
                                </SelectTrigger>
                                <SelectContent>
                                    {Object.values(UserRole)
                                        .filter((r) => r !== UserRole.Student)
                                        .map((r) => (
                                            <SelectItem key={r} value={r}>
                                                {r}
                                            </SelectItem>
                                        ))}
                                </SelectContent>
                            </Select>
                            <AsyncButton
                                onClick={() => assignMutation.mutate(selectedRole)}
                                disabled={isLoading}
                                isLoading={assignMutation.isPending}
                                loadingText={t('common:actions.submitting')}
                            >
                                {t('roleDialogAddBtn')}
                            </AsyncButton>
                        </div>
                    </div>
                </div>

                <DialogFooter>
                    <Button variant="ghost" onClick={onClose}>
                        {t('roleDialogClose')}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
