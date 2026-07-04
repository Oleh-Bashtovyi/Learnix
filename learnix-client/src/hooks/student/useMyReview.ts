import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { reviewsApi } from '@/api/reviews.api';
import { useAuthStore } from '@/store/auth.store';

export function useMyReview(courseId: string) {
    const user = useAuthStore((s) => s.user);

    return useQuery({
        queryKey: queryKeys.reviews.mine(courseId),
        queryFn: () => reviewsApi.getMyReview(courseId),
        enabled: !!courseId && !!user,
    });
}
