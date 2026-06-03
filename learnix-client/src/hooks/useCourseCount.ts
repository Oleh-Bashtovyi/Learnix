import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { coursesApi } from '@/api/courses.api';

export function useCourseCount() {
    return useQuery({
        queryKey: queryKeys.courses.count(),
        queryFn: () => coursesApi.getPublic({ skip: 0, take: 1 }).then((r) => r.totalCount),
        staleTime: 1000 * 60 * 5,
    });
}
