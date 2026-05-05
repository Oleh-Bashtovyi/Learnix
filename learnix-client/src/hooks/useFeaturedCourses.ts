import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { featuredCourses } from '@/mocks/landing.mock';
import type { CourseSummaryDto } from '@/types/course.types';

/**
 * Returns featured courses for the landing page.
 * Uses mock data — the backend GET /api/courses does not yet return
 * rating, reviewsCount, durationHours, instructor.fullName, or badge fields.
 * Replace the queryFn with a real API call once the endpoint is extended.
 */
export function useFeaturedCourses() {
    return useQuery<CourseSummaryDto[]>({
        queryKey: queryKeys.courses.featured(),
        queryFn: () => Promise.resolve(featuredCourses),
        staleTime: Infinity,
    });
}
