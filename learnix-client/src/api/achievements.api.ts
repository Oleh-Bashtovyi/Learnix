import type { GetMyAchievementsResponse } from '@/types/achievement.types';
import { api } from './axios.instance';

export const achievementsApi = {
    getMyAchievements: () =>
        api.get<GetMyAchievementsResponse>('/achievements/me').then((r) => r.data),

    markSeen: (achievementId: string) =>
        api.post<void>(`/achievements/${achievementId}/seen`).then((r) => r.data),
};
