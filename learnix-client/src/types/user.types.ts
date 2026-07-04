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

export interface UpdateProfileRequest {
    firstName: string;
    lastName: string;
    bio: string | null;
    avatarBlobPath: string | null;
}
