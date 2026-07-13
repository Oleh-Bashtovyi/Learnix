/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth (DTOs are defined separately)
 */

export interface MyProfileDto {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    bio: string | null;
    avatarUrl: string | null;
    createdAt: string;
    hasPassword: boolean;
}

export interface UserProfileDto {
    id: string;
    firstName: string;
    lastName: string;
    bio: string | null;
    avatarUrl: string | null;
}

/** An instructor's public profile: who they are, plus what their published courses add up to. */
export interface InstructorProfileDto {
    id: string;
    firstName: string;
    lastName: string;
    bio: string | null;
    avatarUrl: string | null;
    joinedAt: string;
    coursesCount: number;
    /** Enrollments summed across the published courses. */
    totalStudents: number;
    /** Weighted by review count. Zero when nothing has been reviewed. */
    averageRating: number;
    reviewsCount: number;
}

export interface UpdateProfileRequest {
    firstName: string;
    lastName: string;
    bio: string | null;
    avatarBlobPath: string | null;
}
