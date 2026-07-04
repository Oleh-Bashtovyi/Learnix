import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/api/queryKeys';
import { type ReorderItem, sectionsApi } from '@/api/sections.api';

function invalidateEdit(qc: ReturnType<typeof useQueryClient>, courseId: string) {
    qc.invalidateQueries({ queryKey: queryKeys.instructor.courseForEdit(courseId) });
}

export function useCreateSection(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (title: string) => sectionsApi.create(courseId, title),
        onSuccess: () => {
            toast.success(t('toastSectionAdded'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useUpdateSectionTitle(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ sectionId, title }: { sectionId: string; title: string }) =>
            sectionsApi.updateTitle(courseId, sectionId, title),
        onSuccess: () => invalidateEdit(qc, courseId),
        onError: () => toast.error(t('toastError')),
    });
}

export function useDeleteSection(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (sectionId: string) => sectionsApi.delete(courseId, sectionId),
        onSuccess: () => {
            toast.success(t('toastSectionDeleted'));
            invalidateEdit(qc, courseId);
        },
        onError: () => toast.error(t('toastError')),
    });
}

export function useReorderSections(courseId: string) {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (items: ReorderItem[]) => sectionsApi.reorder(courseId, items),
        onSuccess: () => invalidateEdit(qc, courseId),
        onError: () => toast.error(t('toastError')),
    });
}
