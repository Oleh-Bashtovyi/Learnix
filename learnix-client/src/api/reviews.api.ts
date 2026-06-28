import type { PaginatedResult } from '@/types/api.types';
import type {
    CourseReviewDto,
    CreateReviewRequest,
    MyReviewDto,
    UpdateReviewRequest,
} from '@/types/review.types';
import { api } from './axios.instance';

export const reviewsApi = {
    getCourseReviews: (courseId: string, skip = 0, take = 20) =>
        api
            .get<PaginatedResult<CourseReviewDto>>(`/courses/${courseId}/reviews`, {
                params: { skip, take },
            })
            .then((r) => r.data),

    getMyReview: (courseId: string) =>
        api
            .get<MyReviewDto>(`/courses/${courseId}/reviews/mine`)
            .then((r) => r.data)
            .catch(() => null),

    createReview: (courseId: string, data: CreateReviewRequest) =>
        api.post<{ reviewId: string }>(`/courses/${courseId}/reviews`, data).then((r) => r.data),

    updateReview: (courseId: string, reviewId: string, data: UpdateReviewRequest) =>
        api.put<void>(`/courses/${courseId}/reviews/${reviewId}`, data).then((r) => r.data),

    deleteReview: (courseId: string, reviewId: string) =>
        api.delete<void>(`/courses/${courseId}/reviews/${reviewId}`).then((r) => r.data),
};
