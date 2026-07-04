import { APP_ROUTES } from '@/routes/paths';
import type { UserSummary } from '@/store/auth.store';

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-005: Role-Based Routing & Default Entry Points
 */
export function getRoleHome(roles: UserSummary['roles']): string {
    if (roles.includes('Admin')) return APP_ROUTES.admin.dashboard;
    if (roles.includes('Instructor')) return APP_ROUTES.instructor.dashboard;
    return APP_ROUTES.public.courses;
}
