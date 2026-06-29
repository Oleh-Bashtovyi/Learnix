import type { AdminStatsDto, AdminUserDto, PendingApplicationDto } from '@/types/admin.types';
import type { PaginatedResult } from '@/types/api.types';
import type { ManageCourseCardDto } from '@/types/course.types';
import { api } from './axios.instance';

export interface AdminUsersFilters {
    search?: string;
    skip?: number;
    take?: number;
    includeDeleted?: boolean;
}

export interface AdminCoursesFilters {
    search?: string;
    skip?: number;
    take?: number;
    categoryId?: string;
    includeDeleted?: boolean;
}

export interface AdminApplicationsParams {
    skip?: number;
    take?: number;
}

export const adminApi = {
    // Stats

    getStats: () => api.get<AdminStatsDto>('/admin/stats').then((r) => r.data),

    // Users

    getUsers: (params: AdminUsersFilters = {}) =>
        api.get<PaginatedResult<AdminUserDto>>('/admin/users', { params }).then((r) => r.data),

    banUser: (userId: string) => api.post(`/admin/users/${userId}/ban`).then((r) => r.data),

    unbanUser: (userId: string) => api.post(`/admin/users/${userId}/unban`).then((r) => r.data),

    deleteUser: (userId: string) => api.delete(`/admin/users/${userId}`).then((r) => r.data),

    recoverUser: (userId: string) => api.post(`/admin/users/${userId}/recover`).then((r) => r.data),

    assignRole: (userId: string, role: string) =>
        api.post(`/admin/users/${userId}/roles/${role}`).then((r) => r.data),

    removeRole: (userId: string, role: string) =>
        api.delete(`/admin/users/${userId}/roles/${role}`).then((r) => r.data),

    // Courses

    getCourses: (params: AdminCoursesFilters = {}) =>
        api
            .get<PaginatedResult<ManageCourseCardDto>>('/admin/courses', { params })
            .then((r) => r.data),

    publishCourse: (courseId: string) =>
        api.post(`/admin/courses/${courseId}/publish`).then((r) => r.data),

    unpublishCourse: (courseId: string) =>
        api.post(`/admin/courses/${courseId}/unpublish`).then((r) => r.data),

    deleteCourse: (courseId: string) =>
        api.delete(`/admin/courses/${courseId}`).then((r) => r.data),

    recoverCourse: (courseId: string) =>
        api.post(`/admin/courses/${courseId}/recover`).then((r) => r.data),

    // Instructor applications

    getPendingApplications: (params: AdminApplicationsParams = {}) =>
        api
            .get<PaginatedResult<PendingApplicationDto>>('/instructor-applications/pending', {
                params,
            })
            .then((r) => r.data),

    approveApplication: (id: string) =>
        api.post(`/instructor-applications/${id}/approve`).then((r) => r.data),

    rejectApplication: (id: string, rejectionReason: string | null) =>
        api.post(`/instructor-applications/${id}/reject`, { rejectionReason }).then((r) => r.data),
};
