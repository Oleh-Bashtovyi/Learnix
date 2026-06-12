import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import {
    lessonsApi,
    type CreateVideoLessonRequest,
    type CreatePostLessonRequest,
    type CreateTestLessonRequest,
} from '@/api/lessons.api';
import { type ReorderItem } from '@/api/sections.api';
import { queryKeys } from '@/api/queryKeys';

function invalidateEdit(qc: ReturnType<typeof useQueryClient>, courseId: string) {
    qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
}

export function useCreateVideoLesson(courseId: string, sectionId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreateVideoLessonRequest) =>
            lessonsApi.createVideo(courseId, sectionId, data),
        onSuccess: () => {
            toast.success(t('toastLessonSaved'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useCreatePostLesson(courseId: string, sectionId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreatePostLessonRequest) =>
            lessonsApi.createPost(courseId, sectionId, data),
        onSuccess: () => {
            toast.success(t('toastLessonSaved'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useCreateTestLesson(courseId: string, sectionId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreateTestLessonRequest) =>
            lessonsApi.createTest(courseId, sectionId, data),
        onSuccess: () => {
            toast.success(t('toastLessonSaved'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useUpdateVideoLesson(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ lessonId, data }: { lessonId: string; data: CreateVideoLessonRequest }) =>
            lessonsApi.updateVideo(courseId, lessonId, data),
        onSuccess: () => {
            toast.success(t('toastLessonSaved'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useUpdatePostLesson(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ lessonId, data }: { lessonId: string; data: CreatePostLessonRequest }) =>
            lessonsApi.updatePost(courseId, lessonId, data),
        onSuccess: () => {
            toast.success(t('toastLessonSaved'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useUpdateTestLesson(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ lessonId, data }: { lessonId: string; data: CreateTestLessonRequest }) =>
            lessonsApi.updateTest(courseId, lessonId, data),
        onSuccess: () => {
            toast.success(t('toastLessonSaved'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useToggleLessonVisibility(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ lessonId, isHidden }: { lessonId: string; isHidden: boolean }) =>
            lessonsApi.toggleVisibility(courseId, lessonId, isHidden),
        onSuccess: () => {
            toast.success(t('toastLessonSaved'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useDeleteLesson(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (lessonId: string) => lessonsApi.delete(courseId, lessonId),
        onSuccess: () => {
            toast.success(t('toastLessonDeleted'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useReorderLessons(courseId: string, sectionId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (items: ReorderItem[]) => lessonsApi.reorder(courseId, sectionId, items),
        onSuccess: () => invalidateEdit(qc, courseId),
        onError: () => toast.error(t('toastError')),
    });
}
