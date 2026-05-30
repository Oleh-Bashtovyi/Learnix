export interface MyProfileDto {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    bio: string | null;
    avatarUrl: string | null;
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
