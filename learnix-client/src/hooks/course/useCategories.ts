import { useQuery } from '@tanstack/react-query';
import { type CategoryListItemDto, categoriesApi } from '@/api/categories.api';
import { queryKeys } from '@/api/queryKeys';
import { RARELY_CHANGING_STALE_TIME } from '@/const/ui.constants';

export function useCategories() {
    return useQuery<CategoryListItemDto[]>({
        queryKey: queryKeys.categories.lists(),
        queryFn: () => categoriesApi.getAll(),
        staleTime: RARELY_CHANGING_STALE_TIME,
    });
}
