import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';
import type { PaginatedResult } from '@/types/api.types';
import type { CourseSummaryDto } from '@/types/course.types';

export function useInstructorCourses(instructorId: string, page: number, pageSize: number) {
    return useQuery<PaginatedResult<CourseSummaryDto>>({
        queryKey: queryKeys.courses.list({ instructorId, page, pageSize }),
        queryFn: async () => {
            const data = await coursesApi.getPublic({
                instructorId,
                skip: (page - 1) * pageSize,
                take: pageSize,
            });
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
        // A page change is a new query key, so without this the query would fall back to pending and
        // the page would collapse into its skeleton — header and all — on every click of "next".
        // Keeping the previous page on screen while the next one loads is what makes it read as paging
        // through a list rather than as reloading the profile.
        placeholderData: (previous) => previous,
        staleTime: 1000 * 60,
    });
}
