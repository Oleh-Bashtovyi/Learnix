import { UserRole } from '@/enums/user.enums';
import type { UserSummary } from '@/store/auth.store';

/**
 * Roles are additive — an instructor is almost always a student too, so anything gated on
 * "not yet an instructor" has to ask this, not whether the user is a student.
 *
 * Admins are included because the API rejects their applications outright
 * (see SubmitApplicationCommandHandler).
 */
export function isInstructorOrAdmin(user: UserSummary | null | undefined): boolean {
    if (!user) return false;
    return user.roles.includes(UserRole.Instructor) || user.roles.includes(UserRole.Admin);
}

/**
 * Specifically Instructor, not Admin: only an Instructor account has a public `/instructors/{id}`
 * page — an admin who never applied has nothing there to link to.
 */
export function isInstructor(user: UserSummary | null | undefined): boolean {
    if (!user) return false;
    return user.roles.includes(UserRole.Instructor);
}
