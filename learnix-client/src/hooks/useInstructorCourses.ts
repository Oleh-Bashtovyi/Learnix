import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';
import type { PaginatedResult } from '@/types/api.types';
import type { CourseSummaryDto } from '@/types/course.types';

export function useInstructorCourses(instructorId: string) {
    return useQuery<PaginatedResult<CourseSummaryDto>>({
        queryKey: queryKeys.courses.list({ instructorId }),
        queryFn: async () => {
            const data = await coursesApi.getPublic({ instructorId, take: 50 });
            return {
                ...data,
                items: data.items.map(
                    (c): CourseSummaryDto => ({
                        id: c.id,
                        title: c.title,
                        description: c.description,
                        coverImageUrl: c.coverImageUrl,
                        price: c.price,
                        rating: c.averageRating,
                        reviewsCount: c.reviewsCount,
                        durationHours: 0,
                        categoryName: c.categoryName,
                        instructor: { id: c.instructorId, fullName: c.instructorFullName },
                        badge: null,
                    }),
                ),
            };
        },
        staleTime: 1000 * 60,
    });
}
