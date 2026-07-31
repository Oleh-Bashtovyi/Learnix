import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import axios from 'axios';
import { Loader2, X } from 'lucide-react';
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
import { env } from '@/utils/env';
import { parseAccessToken } from '@/utils/parseAccessToken';

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
    const setAccessToken = useAuthStore((s) => s.setAccessToken);
    const setUser = useAuthStore((s) => s.setUser);

    const [selectedRole, setSelectedRole] = useState<string>(UserRole.Instructor);

    const refreshSelfIfNeeded = async () => {
        if (user.id !== currentUser?.id) return;
        try {
            const { data } = await axios.post<{ accessToken: string; avatarUrl: string | null }>(
                `${env.API_URL}/auth/refresh`,
                {},
                { withCredentials: true },
            );
            setAccessToken(data.accessToken);
            const updatedUser = parseAccessToken(data.accessToken);
            if (updatedUser) setUser({ ...updatedUser, avatarUrl: data.avatarUrl });
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
                                                <button
                                                    onClick={() => removeMutation.mutate(role)}
                                                    disabled={isLoading}
                                                    className="ml-0.5 opacity-60 transition-opacity hover:opacity-100 disabled:cursor-not-allowed"
                                                    title={t('roleDialogRemoveRole', { role })}
                                                >
                                                    {removeMutation.isPending &&
                                                    removeMutation.variables === role ? (
                                                        <Loader2
                                                            size={10}
                                                            className="animate-spin"
                                                        />
                                                    ) : (
                                                        <X size={10} />
                                                    )}
                                                </button>
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
