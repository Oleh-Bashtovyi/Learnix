import { useMutation, useQueryClient } from '@tanstack/react-query';
import { achievementsApi } from '@/api/achievements.api';
import { queryKeys } from '@/api/queryKeys';

export function useMarkAchievementSeen() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: achievementsApi.markSeen,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });
        },
    });
}
