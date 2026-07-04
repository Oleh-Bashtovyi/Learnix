import type {
    GetTestLessonDto,
    StartAttemptResponse,
    SubmitAttemptRequest,
    SubmitAttemptResponse,
    TestAttemptSummaryDto,
} from '@/types/lesson.types';
import { api } from './axios.instance';

export const testsApi = {
    getTestLesson: (courseId: string, lessonId: string) =>
        api
            .get<GetTestLessonDto>(`/courses/${courseId}/lessons/${lessonId}/test`)
            .then((r) => r.data),

    startAttempt: (courseId: string, lessonId: string) =>
        api
            .post<StartAttemptResponse>(
                `/courses/${courseId}/lessons/${lessonId}/test/attempts/start`,
            )
            .then((r) => r.data),

    submitAttempt: (
        courseId: string,
        lessonId: string,
        attemptId: string,
        data: SubmitAttemptRequest,
    ) =>
        api
            .post<SubmitAttemptResponse>(
                `/courses/${courseId}/lessons/${lessonId}/test/attempts/${attemptId}/submit`,
                data,
            )
            .then((r) => r.data),

    getMyAttempts: (courseId: string, lessonId: string) =>
        api
            .get<TestAttemptSummaryDto[]>(`/courses/${courseId}/lessons/${lessonId}/test/attempts`)
            .then((r) => r.data),
};
