import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';

/**
 * Related ADRs:
 * - ADR-FRONT-API-007: Data Fetching Abstraction (Custom Hooks)
 */
export function useCourseDetail(courseId: string) {
    return useQuery({
        queryKey: queryKeys.courses.detail(courseId),
        queryFn: () => coursesApi.getById(courseId),
        enabled: !!courseId,
    });
}
