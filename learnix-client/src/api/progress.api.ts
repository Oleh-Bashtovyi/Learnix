import type { CourseProgressDto, MarkLessonCompleteResponse } from '@/types/progress.types';
import { api } from './axios.instance';

export const progressApi = {
    getCourseProgress: (courseId: string) =>
        api.get<CourseProgressDto>(`/progress/courses/${courseId}`).then((r) => r.data),

    markLessonComplete: (courseId: string, lessonId: string) =>
        api
            .post<MarkLessonCompleteResponse>(
                `/progress/courses/${courseId}/lessons/${lessonId}/complete`,
            )
            .then((r) => r.data),
};
