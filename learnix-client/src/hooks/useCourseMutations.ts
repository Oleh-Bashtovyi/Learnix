import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { coursesApi, type CreateCourseRequest, type UpdateCourseRequest } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';

function invalidateMyCourses(qc: ReturnType<typeof useQueryClient>) {
    qc.invalidateQueries({ queryKey: ['courses', 'mine'] });
}

export function useCreateCourse() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: CreateCourseRequest) => coursesApi.create(data),
        onSuccess: () => invalidateMyCourses(qc),
        onError: () => toast.error(t('toastError')),
    });
}

export function useUpdateCourse(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: UpdateCourseRequest) => coursesApi.update(courseId, data),
        onSuccess: () => {
            toast.success(t('toastCourseSaved'));
            qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function usePublishCourse() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.publish(courseId),
        onSuccess: (_data, courseId) => {
            toast.success(t('toastCoursePublished'));
            qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useUnpublishCourse() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.unpublish(courseId),
        onSuccess: (_data, courseId) => {
            toast.success(t('toastCourseUnpublished'));
            qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useArchiveCourse() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.archive(courseId),
        onSuccess: () => {
            toast.success(t('toastCourseArchived'));
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useUnarchiveCourse() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.unarchive(courseId),
        onSuccess: () => {
            toast.success(t('toastCourseUnarchived'));
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useDeleteCourse() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => coursesApi.deleteCourse(courseId),
        onSuccess: () => {
            toast.success(t('toastCourseDeleted'));
            invalidateMyCourses(qc);
        },
        onError: () => toast.error(t('toastError')),
    });
}
