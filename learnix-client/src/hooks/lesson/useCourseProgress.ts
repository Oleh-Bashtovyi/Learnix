import { useQuery } from '@tanstack/react-query';
import { progressApi } from '@/api/progress.api';
import { queryKeys } from '@/api/queryKeys';

export function useCourseProgress(courseId: string) {
    return useQuery({
        queryKey: queryKeys.progress.course(courseId),
        queryFn: () => progressApi.getCourseProgress(courseId),
        enabled: !!courseId,
    });
}
