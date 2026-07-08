export const UserRole = {
    Student: 'Student',
    Instructor: 'Instructor',
    Admin: 'Admin',
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];
