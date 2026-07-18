import { useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { testsApi } from '@/api/tests.api';
import type { SubmitAttemptRequest } from '@/types/lesson.types';

export function useSubmitTestAttempt(courseId: string, lessonId: string) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ attemptId, data }: { attemptId: string; data: SubmitAttemptRequest }) =>
            testsApi.submitAttempt(courseId, lessonId, attemptId, data),
        onSuccess: () => {
            queryClient.invalidateQueries({
                queryKey: queryKeys.tests.lesson(courseId, lessonId),
            });
            queryClient.invalidateQueries({
                queryKey: queryKeys.tests.attempts(courseId, lessonId),
            });
            queryClient.invalidateQueries({
                queryKey: queryKeys.progress.course(courseId),
            });
            // Passing a test can complete the lesson (and the course), which the My Learning cards
            // read from the enrollments list — refresh it so progress and completion stay in sync.
            queryClient.invalidateQueries({
                queryKey: queryKeys.enrollments.mine(),
            });
            queryClient.invalidateQueries({
                queryKey: queryKeys.enrollments.continueLearning(),
            });
            queryClient.invalidateQueries({
                queryKey: queryKeys.certificates.mine(),
            });
        },
    });
}
