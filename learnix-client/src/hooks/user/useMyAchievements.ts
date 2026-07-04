import { useQuery } from '@tanstack/react-query';
import { achievementsApi } from '@/api/achievements.api';
import { queryKeys } from '@/api/queryKeys';

export function useMyAchievements() {
    return useQuery({
        queryKey: queryKeys.achievements.mine(),
        queryFn: achievementsApi.getMyAchievements,
    });
}
