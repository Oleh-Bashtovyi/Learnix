import { useQuery } from '@tanstack/react-query';
import { lessonsApi } from '@/api/lessons.api';
import { queryKeys } from '@/api/queryKeys';

export function useLessonContent(courseId: string, lessonId: string) {
    return useQuery({
        queryKey: queryKeys.lessons.content(courseId, lessonId),
        queryFn: () => lessonsApi.getContent(courseId, lessonId),
        enabled: !!courseId && !!lessonId,
    });
}
