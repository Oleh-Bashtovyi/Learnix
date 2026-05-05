import { api } from './axios.instance';

/** Matches backend CategoryListItemDto */
export interface CategoryListItemDto {
    id: string;
    name: string;
    slug: string;
    imageUrl: string | null;
}

export const categoriesApi = {
    getAll: () => api.get<CategoryListItemDto[]>('/categories').then((r) => r.data),
};
