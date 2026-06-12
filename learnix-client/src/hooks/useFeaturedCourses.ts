import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { coursesApi } from '@/api/courses.api';
import type { CourseSummaryDto } from '@/types/course.types';

export function useFeaturedCourses() {
    return useQuery<CourseSummaryDto[]>({
        queryKey: queryKeys.courses.featured(),
        queryFn: async () => {
            const data = await coursesApi.getFeatured();
            return data.map(
                (c): CourseSummaryDto => ({
                    id: c.id,
                    title: c.title,
                    description: c.description,
                    coverImageUrl: c.coverImageUrl,
                    price: c.price,
                    rating: c.rating,
                    reviewsCount: c.reviewsCount,
                    durationHours: c.durationHours,
                    categoryName: c.categoryName,
                    instructor: c.instructor,
                    badge: c.badge,
                }),
            );
        },
        staleTime: 1000 * 60 * 5,
    });
}
