namespace Learnix.Application.Achievements.Queries.GetMyAchievements;

public sealed record GetMyAchievementsResponse(
    IReadOnlyList<UnlockedAchievementDto> Unlocked,
    AchievementProgressDto Progress);

public sealed record UnlockedAchievementDto(
    Guid Id,
    string Code,
    DateTime UnlockedAt,
    bool Seen);

public sealed record AchievementProgressDto(
    int LessonsCompleted,
    int CoursesCompleted,
    int DistinctCategoriesCompleted,
    bool ProfileCompleted);
