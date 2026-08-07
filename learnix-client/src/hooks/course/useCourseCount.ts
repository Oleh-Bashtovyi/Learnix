import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';
import { RARELY_CHANGING_STALE_TIME } from '@/const/ui.constants';

export function useCourseCount() {
    return useQuery({
        queryKey: queryKeys.courses.count(),
        queryFn: () => coursesApi.getPublic({ skip: 0, take: 1 }).then((r) => r.totalCount),
        staleTime: RARELY_CHANGING_STALE_TIME,
    });
}
