import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { usersApi } from '@/api/users.api';

export function useMyProfile() {
    return useQuery({
        queryKey: queryKeys.users.myProfile(),
        queryFn: usersApi.getMyProfile,
    });
}
