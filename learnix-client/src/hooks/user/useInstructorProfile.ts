import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { usersApi } from '@/api/users.api';

/** The instructor's public profile and the aggregates over their published courses. */
export function useInstructorProfile(instructorId: string) {
    return useQuery({
        queryKey: queryKeys.users.instructorProfile(instructorId),
        queryFn: () => usersApi.getInstructorProfile(instructorId),
        staleTime: 1000 * 60 * 5,
    });
}
