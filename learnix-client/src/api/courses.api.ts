import { api } from './axios.instance';
import type { PaginatedResult } from '@/types/api.types';

export interface CourseFilters {
    search?: string;
    skip?: number;
    take?: number;
    categoryId?: string;
    instructorId?: string;
}

/** Matches backend PublicCourseCardDto */
export interface PublicCourseCardDto {
    id: string;
    instructorId: string;
    categoryId: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    enrollmentsCount: number;
    tags: string[];
}

export const coursesApi = {
    getPublic: (filters: CourseFilters = {}) =>
        api
            .get<PaginatedResult<PublicCourseCardDto>>('/courses', { params: filters })
            .then((r) => r.data),
};
