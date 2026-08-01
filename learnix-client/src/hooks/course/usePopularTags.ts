import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';

/**
 * Tag suggestions for the course editor. The server caches the list for an hour, so a long
 * staleTime here only mirrors what a refetch would return anyway.
 */
export function usePopularTags(categoryId?: string) {
    return useQuery<string[]>({
        queryKey: queryKeys.courses.popularTags(categoryId),
        queryFn: () => coursesApi.getPopularTags(categoryId),
        staleTime: 1000 * 60 * 30,
    });
}
