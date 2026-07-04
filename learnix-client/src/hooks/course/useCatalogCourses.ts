import { useQuery } from '@tanstack/react-query';
import { coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';
import type { PaginatedResult } from '@/types/api.types';
import type { CourseSummaryDto } from '@/types/course.types';

export interface CatalogFilters {
    search: string;
    categoryId: string;
    sortBy: 'popular' | 'newest' | 'rating';
    isFree: boolean | undefined;
    minRating: number | undefined;
    page: number;
    pageSize: number;
}

export function useCatalogCourses(filters: CatalogFilters) {
    const { search, categoryId, sortBy, isFree, minRating, page, pageSize } = filters;

    return useQuery<PaginatedResult<CourseSummaryDto>>({
        queryKey: queryKeys.courses.list({
            search,
            categoryId,
            sortBy,
            isFree,
            minRating,
            page,
            pageSize,
        }),
        queryFn: async () => {
            const data = await coursesApi.getPublic({
                search: search || undefined,
                categoryId: categoryId || undefined,
                sortBy: sortBy === 'popular' ? undefined : sortBy,
                isFree,
                minRating,
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
        staleTime: 1000 * 30,
        placeholderData: (prev) => prev,
    });
}
