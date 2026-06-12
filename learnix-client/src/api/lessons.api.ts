import { api } from './axios.instance';
import type { LessonContentDto } from '@/types/lesson.types';
import type { ReorderItem } from './sections.api';

export interface CreateVideoLessonRequest {
    title: string;
    videoUrl: string;
    description?: string;
    durationSeconds?: number;
}

export interface CreatePostLessonRequest {
    title: string;
    content: string;
}

export interface QuestionOptionRequest {
    text: string;
    isCorrect: boolean;
}

export interface QuestionRequest {
    text: string;
    type: 'SingleChoice' | 'MultipleChoice';
    options: QuestionOptionRequest[];
}

export interface CreateTestLessonRequest {
    title: string;
    description?: string;
    attemptLimit?: number;
    cooldownMinutes?: number;
    passingThreshold: number;
    questions: QuestionRequest[];
}

export const lessonsApi = {
    getContent: (courseId: string, lessonId: string) =>
        api.get<LessonContentDto>(`/courses/${courseId}/lessons/${lessonId}`).then((r) => r.data),

    // Instructor methods
    createVideo: (courseId: string, sectionId: string, data: CreateVideoLessonRequest) =>
        api
            .post<{ id: string }>(`/courses/${courseId}/sections/${sectionId}/lessons/video`, data)
            .then((r) => r.data),

    createPost: (courseId: string, sectionId: string, data: CreatePostLessonRequest) =>
        api
            .post<{ id: string }>(`/courses/${courseId}/sections/${sectionId}/lessons/post`, data)
            .then((r) => r.data),

    createTest: (courseId: string, sectionId: string, data: CreateTestLessonRequest) =>
        api
            .post<{ id: string }>(`/courses/${courseId}/sections/${sectionId}/lessons/test`, data)
            .then((r) => r.data),

    updateVideo: (courseId: string, lessonId: string, data: CreateVideoLessonRequest) =>
        api.patch(`/courses/${courseId}/lessons/${lessonId}/video`, data).then((r) => r.data),

    updatePost: (courseId: string, lessonId: string, data: CreatePostLessonRequest) =>
        api.patch(`/courses/${courseId}/lessons/${lessonId}/post`, data).then((r) => r.data),

    updateTest: (courseId: string, lessonId: string, data: CreateTestLessonRequest) =>
        api.patch(`/courses/${courseId}/lessons/${lessonId}/test`, data).then((r) => r.data),

    toggleVisibility: (courseId: string, lessonId: string, isHidden: boolean) =>
        api
            .patch(`/courses/${courseId}/lessons/${lessonId}/toggle-visibility`, { isHidden })
            .then((r) => r.data),

    delete: (courseId: string, lessonId: string) =>
        api.delete(`/courses/${courseId}/lessons/${lessonId}`).then((r) => r.data),

    reorder: (courseId: string, sectionId: string, items: ReorderItem[]) =>
        api
            .post(`/courses/${courseId}/sections/${sectionId}/lessons/reorder`, { items })
            .then((r) => r.data),
};
