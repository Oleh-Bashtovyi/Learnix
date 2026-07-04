import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { usersApi } from '@/api/users.api';

export function useUserProfile(userId: string) {
    return useQuery({
        queryKey: queryKeys.users.profile(userId),
        queryFn: () => usersApi.getUserProfile(userId),
        staleTime: 1000 * 60 * 5,
    });
}
