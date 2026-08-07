import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { usersApi } from '@/api/users.api';
import { RARELY_CHANGING_STALE_TIME } from '@/const/ui.constants';

export function useUserProfile(userId: string) {
    return useQuery({
        queryKey: queryKeys.users.profile(userId),
        queryFn: () => usersApi.getUserProfile(userId),
        staleTime: RARELY_CHANGING_STALE_TIME,
    });
}
