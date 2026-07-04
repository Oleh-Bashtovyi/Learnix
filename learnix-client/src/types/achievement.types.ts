export interface UnlockedAchievementDto {
    id: string;
    code: string;
    unlockedAt: string;
    seen: boolean;
}

export interface AchievementProgressDto {
    lessonsCompleted: number;
    coursesCompleted: number;
    distinctCategoriesCompleted: number;
    profileCompleted: boolean;
}

export interface GetMyAchievementsResponse {
    unlocked: UnlockedAchievementDto[];
    progress: AchievementProgressDto;
}
