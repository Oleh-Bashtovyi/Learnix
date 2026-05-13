import type { UserSummary } from '@/store/auth.store';

export function getRoleHome(role: UserSummary['role']): string {
    if (role === 'Instructor') return '/instructor';
    if (role === 'Admin') return '/admin';
    return '/courses';
}
