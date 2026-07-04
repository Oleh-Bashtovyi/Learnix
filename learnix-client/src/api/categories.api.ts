import { api } from './axios.instance';

/** Matches backend CategoryListItemDto (public endpoint) */
export interface CategoryListItemDto {
    id: string;
    name: string;
    slug: string;
    imageUrl: string | null;
    coursesCount: number;
}

/** Matches backend AdminCategoryListItemDto (admin endpoint) */
export interface AdminCategoryListItemDto {
    id: string;
    name: string;
    slug: string;
    imageUrl: string | null;
    isSystem: boolean;
    coursesCount: number;
}

export interface CreateCategoryRequest {
    name: string;
    slug: string;
}

export interface UpdateCategoryRequest {
    name: string;
    slug: string;
}

export const categoriesApi = {
    getAll: () => api.get<CategoryListItemDto[]>('/categories').then((r) => r.data),
    getAllForAdmin: () =>
        api.get<AdminCategoryListItemDto[]>('/categories/admin').then((r) => r.data),
    create: (data: CreateCategoryRequest) =>
        api.post<{ id: string }>('/categories', data).then((r) => r.data),
    update: (id: string, data: UpdateCategoryRequest) =>
        api.put(`/categories/${id}`, data).then((r) => r.data),
    delete: (id: string) => api.delete(`/categories/${id}`).then((r) => r.data),
    setImage: (id: string, blobPath: string) =>
        api.post(`/categories/${id}/image`, { blobPath }).then((r) => r.data),
    deleteImage: (id: string) => api.delete(`/categories/${id}/image`).then((r) => r.data),
};
