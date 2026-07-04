import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { testsApi } from '@/api/tests.api';

export function useTestLesson(courseId: string, lessonId: string) {
    return useQuery({
        queryKey: queryKeys.tests.lesson(courseId, lessonId),
        queryFn: () => testsApi.getTestLesson(courseId, lessonId),
        enabled: !!courseId && !!lessonId,
    });
}
