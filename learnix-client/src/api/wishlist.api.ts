// learnix-client/src/api/wishlist.api.ts
import type { PaginatedResult } from '@/types/api.types';
import type { WishlistCourseDto } from '@/types/wishlist.types';
import { api } from './axios.instance';

export const wishlistApi = {
    getMine: (skip: number = 0, take: number = 20) =>
        api
            .get<PaginatedResult<WishlistCourseDto>>('/wishlist', { params: { skip, take } })
            .then((r) => r.data),

    add: (courseId: string) => api.post<void>(`/wishlist/${courseId}`).then((r) => r.data),

    remove: (courseId: string) => api.delete<void>(`/wishlist/${courseId}`).then((r) => r.data),
};
