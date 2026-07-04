import { useQuery } from '@tanstack/react-query';
import { configApi } from '@/api/config.api';
import { queryKeys } from '@/api/queryKeys';

export function usePublicConfig() {
    return useQuery({
        queryKey: queryKeys.config.public(),
        queryFn: configApi.getPublicConfig,
        staleTime: Infinity, // Configuration shouldn't change during a user's session
        gcTime: Infinity,
    });
}
