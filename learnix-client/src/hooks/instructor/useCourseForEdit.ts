import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';

export function useCourseForEdit(courseId: string | undefined) {
    return useQuery({
        queryKey: queryKeys.instructor.courseForEdit(courseId ?? ''),
        queryFn: () => coursesApi.getForEdit(courseId!),
        enabled: !!courseId,
    });
}
