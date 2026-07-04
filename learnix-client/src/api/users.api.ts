import type { MyProfileDto, UpdateProfileRequest, UserProfileDto } from '@/types/user.types';
import { api } from './axios.instance';

export const usersApi = {
    getMyProfile: () => api.get<MyProfileDto>('/users/me').then((r) => r.data),

    updateProfile: (data: UpdateProfileRequest) =>
        api.put<void>('/users/me', data).then((r) => r.data),

    getUserProfile: (userId: string) =>
        api.get<UserProfileDto>(`/users/${userId}`).then((r) => r.data),
};
