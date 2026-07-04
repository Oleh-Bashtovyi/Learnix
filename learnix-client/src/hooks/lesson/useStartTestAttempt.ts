import { useMutation } from '@tanstack/react-query';
import { testsApi } from '@/api/tests.api';

export function useStartTestAttempt(courseId: string, lessonId: string) {
    return useMutation({
        mutationFn: () => testsApi.startAttempt(courseId, lessonId),
    });
}
