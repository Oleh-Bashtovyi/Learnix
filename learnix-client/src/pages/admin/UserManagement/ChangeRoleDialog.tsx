import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import axios from 'axios';
import { X } from 'lucide-react';
import { toast } from 'sonner';
import { adminApi } from '@/api/admin.api';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { useAuthStore } from '@/store/auth.store';
import type { AdminUserDto } from '@/types/admin.types';
import { cn } from '@/utils/cn';
import { env } from '@/utils/env';
import { parseAccessToken } from '@/utils/parseAccessToken';

const ALL_ROLES = ['Student', 'Instructor', 'Admin'] as const;

const ROLE_STYLES: Record<string, string> = {
    Student: 'bg-primary/10 text-primary',
    Instructor: 'bg-accent/10 text-accent',
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

    const [selectedRole, setSelectedRole] = useState<string>('Instructor');

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
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <div className="w-full max-w-sm rounded-xl border border-border bg-card shadow-lg">
                {/* Header */}
                <div className="flex items-center justify-between border-b border-border px-5 py-4">
                    <h2 className="font-heading font-semibold text-foreground">
                        {t('roleDialogTitle')}
                    </h2>
                    <button
                        onClick={onClose}
                        className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                    >
                        <X size={16} />
                    </button>
                </div>

                {/* Body */}
                <div className="space-y-4 px-5 py-4">
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
                                        {role !== 'Student' &&
                                            !(role === 'Admin' && user.id === currentUser?.id) && (
                                                <button
                                                    onClick={() => removeMutation.mutate(role)}
                                                    disabled={isLoading}
                                                    className="ml-0.5 opacity-60 transition-opacity hover:opacity-100 disabled:cursor-not-allowed"
                                                    title={`Remove ${role}`}
                                                >
                                                    <X size={10} />
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
                                <SelectTrigger className="flex-1">
                                    <SelectValue
                                        placeholder={t(
                                            'roleDialogSelectPlaceholder',
                                            'Select role',
                                        )}
                                    />
                                </SelectTrigger>
                                <SelectContent>
                                    {ALL_ROLES.filter((r) => r !== 'Student').map((r) => (
                                        <SelectItem key={r} value={r}>
                                            {r}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            <button
                                onClick={() => assignMutation.mutate(selectedRole)}
                                disabled={isLoading}
                                className="rounded-lg bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-50"
                            >
                                {t('roleDialogAddBtn')}
                            </button>
                        </div>
                    </div>
                </div>

                {/* Footer */}
                <div className="flex justify-end border-t border-border px-5 py-3">
                    <button
                        onClick={onClose}
                        className="rounded-lg px-4 py-1.5 text-sm text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                    >
                        {t('roleDialogClose')}
                    </button>
                </div>
            </div>
        </div>
    );
}
