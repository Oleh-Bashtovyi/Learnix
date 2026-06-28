import { useQuery } from '@tanstack/react-query';
import { type InstructorCourseFilters, coursesApi } from '@/api/courses.api';
import { queryKeys } from '@/api/queryKeys';

export function useMyCoursesQuery(filters: InstructorCourseFilters = {}) {
    return useQuery({
        queryKey: queryKeys.instructor.myCourses(filters as Record<string, unknown>),
        queryFn: () => coursesApi.getMine(filters),
    });
}
