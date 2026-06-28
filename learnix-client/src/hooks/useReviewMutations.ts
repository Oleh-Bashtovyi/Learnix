import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/api/queryKeys';
import { reviewsApi } from '@/api/reviews.api';
import type { CreateReviewRequest, UpdateReviewRequest } from '@/types/review.types';

export function useCreateReview(courseId: string) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (data: CreateReviewRequest) => reviewsApi.createReview(courseId, data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.byCourse(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.mine(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.courses.detail(courseId) });
            toast.success('Review submitted');
        },
    });
}

export function useUpdateReview(courseId: string, reviewId: string) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (data: UpdateReviewRequest) =>
            reviewsApi.updateReview(courseId, reviewId, data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.byCourse(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.mine(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.courses.detail(courseId) });
            toast.success('Review updated');
        },
    });
}

export function useDeleteReview(courseId: string, reviewId: string) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: () => reviewsApi.deleteReview(courseId, reviewId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.byCourse(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.reviews.mine(courseId) });
            queryClient.invalidateQueries({ queryKey: queryKeys.courses.detail(courseId) });
            toast.success('Review deleted');
        },
    });
}
