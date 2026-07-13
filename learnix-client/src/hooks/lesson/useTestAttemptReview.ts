import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { testsApi } from '@/api/tests.api';

/** Replays one of the student's own submitted attempts. Only fetched once an attempt is picked. */
export function useTestAttemptReview(courseId: string, lessonId: string, attemptId: string | null) {
    return useQuery({
        queryKey: queryKeys.tests.attemptReview(courseId, lessonId, attemptId ?? ''),
        queryFn: () => testsApi.getAttemptReview(courseId, lessonId, attemptId!),
        enabled: attemptId !== null,
    });
}
