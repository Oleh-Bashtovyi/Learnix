import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { reviewsApi } from '@/api/reviews.api';

export function useCourseReviews(courseId: string, skip = 0, take = 20) {
    return useQuery({
        queryKey: [...queryKeys.reviews.byCourse(courseId), skip, take],
        queryFn: () => reviewsApi.getCourseReviews(courseId, skip, take),
        enabled: !!courseId,
    });
}
