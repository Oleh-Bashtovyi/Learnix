using Learnix.Domain.Common;

namespace Learnix.Domain.Entities;

/// <summary>
/// Denormalized cache of per-user counters for the achievements page.
/// Reconciled (SET, not increment) by the achievement evaluator after each
/// triggering event so retries stay idempotent.
/// </summary>
public class UserAchievementProgress : IAuditable
{
    private UserAchievementProgress() { }

    private UserAchievementProgress(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; private set; }
    public int LessonsCompleted { get; private set; }
    public int CoursesCompleted { get; private set; }
    public int DistinctCategoriesCompleted { get; private set; }
    public bool ProfileCompleted { get; private set; }

#pragma warning disable S1144
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
#pragma warning restore S1144

    public static UserAchievementProgress Create(Guid userId) => new(userId);

    public void SetLessonsCompleted(int value) => LessonsCompleted = value;
    public void SetCoursesCompleted(int value) => CoursesCompleted = value;
    public void SetDistinctCategoriesCompleted(int value) => DistinctCategoriesCompleted = value;
    public void SetProfileCompleted(bool value) => ProfileCompleted = value;
}
