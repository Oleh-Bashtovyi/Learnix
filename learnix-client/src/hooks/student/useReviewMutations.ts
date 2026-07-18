import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/api/queryKeys';
import { reviewsApi } from '@/api/reviews.api';
import type { CreateReviewRequest, UpdateReviewRequest } from '@/types/review.types';

export function useCreateReview(courseId: string) {
    const queryClient = useQueryClient();
    const { t } = useTranslation('courseDetail');

    return useMutation({
        mutationFn: (data: CreateReviewRequest) => reviewsApi.createReview(courseId, data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.byCourse(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.mine(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.courses.detail(courseId) });
            // My Learning cards show the student's own rating, so refresh the enrollments list too.
            queryClient.invalidateQueries({ queryKey: queryKeys.enrollments.mine() });
            toast.success(t('reviews.submitted'));
        },
    });
}

export function useUpdateReview(courseId: string, reviewId: string) {
    const queryClient = useQueryClient();
    const { t } = useTranslation('courseDetail');

    return useMutation({
        mutationFn: (data: UpdateReviewRequest) =>
            reviewsApi.updateReview(courseId, reviewId, data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.byCourse(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.mine(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.courses.detail(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.enrollments.mine() });
            toast.success(t('reviews.updated'));
        },
    });
}

export function useDeleteReview(courseId: string, reviewId: string) {
    const queryClient = useQueryClient();
    const { t } = useTranslation('courseDetail');

    return useMutation({
        mutationFn: () => reviewsApi.deleteReview(courseId, reviewId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.byCourse(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.mine(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.courses.detail(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.enrollments.mine() });
            toast.success(t('reviews.deleted'));
        },
    });
}
