import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { usersApi } from '@/api/users.api';
import { RARELY_CHANGING_STALE_TIME } from '@/const/ui.constants';

/** The instructor's public profile and the aggregates over their published courses. */
export function useInstructorProfile(instructorId: string) {
    return useQuery({
        queryKey: queryKeys.users.instructorProfile(instructorId),
        queryFn: () => usersApi.getInstructorProfile(instructorId),
        staleTime: RARELY_CHANGING_STALE_TIME,
    });
}
