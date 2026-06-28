import type { PaginatedResult } from '@/types/api.types';
import type { CourseDetailDto, CourseForEditDto, ManageCourseCardDto } from '@/types/course.types';
import { api } from './axios.instance';

export interface CourseFilters {
    search?: string;
    skip?: number;
    take?: number;
    categoryId?: string;
    instructorId?: string;
    sortBy?: 'popular' | 'newest' | 'rating';
    isFree?: boolean;
    minRating?: number;
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
    averageRating: number;
    reviewsCount: number;
    categoryName: string;
    instructorFullName: string;
}

/** Matches backend FeaturedCourseDto */
export interface FeaturedCourseCardDto {
    id: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    rating: number;
    reviewsCount: number;
    durationHours: number;
    categoryName: string;
    instructor: { id: string; fullName: string };
    badge: 'bestseller' | 'new' | null;
}

export interface InstructorCourseFilters {
    search?: string;
    skip?: number;
    take?: number;
    categoryId?: string;
}

export interface CreateCourseRequest {
    categoryId: string;
    title: string;
    description: string;
    price: number;
    tags?: string[];
}

export interface UpdateCourseRequest {
    categoryId: string;
    title: string;
    description: string;
    price: number;
    coverImageUrl: string | null;
    tags: string[];
}

export const coursesApi = {
    getPublic: (filters: CourseFilters = {}) =>
        api
            .get<PaginatedResult<PublicCourseCardDto>>('/courses', { params: filters })
            .then((r) => r.data),

    getFeatured: () => api.get<FeaturedCourseCardDto[]>('/courses/featured').then((r) => r.data),

    getById: (id: string) => api.get<CourseDetailDto>(`/courses/${id}`).then((r) => r.data),

    // Instructor methods
    getMine: (filters: InstructorCourseFilters = {}) =>
        api
            .get<PaginatedResult<ManageCourseCardDto>>('/courses/mine', { params: filters })
            .then((r) => r.data),

    getForEdit: (id: string) =>
        api.get<CourseForEditDto>(`/courses/${id}/edit`).then((r) => r.data),

    create: (data: CreateCourseRequest) =>
        api.post<{ courseId: string }>('/courses', data).then((r) => r.data),

    update: (id: string, data: UpdateCourseRequest) =>
        api.put(`/courses/${id}`, data).then((r) => r.data),

    publish: (id: string) => api.post(`/courses/${id}/publish`).then((r) => r.data),

    unpublish: (id: string) => api.post(`/courses/${id}/unpublish`).then((r) => r.data),

    archive: (id: string) => api.post(`/courses/${id}/archive`).then((r) => r.data),

    unarchive: (id: string) => api.post(`/courses/${id}/unarchive`).then((r) => r.data),

    deleteCourse: (id: string) => api.delete(`/courses/${id}`).then((r) => r.data),
};
