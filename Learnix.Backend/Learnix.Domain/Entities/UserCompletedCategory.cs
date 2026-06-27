using Learnix.Domain.Common;

namespace Learnix.Domain.Entities;

/// <summary>
/// Tracks which course categories a user has completed at least one course in.
/// Drives the "courses from N different categories" achievement.
/// Composite PK (UserId, CategoryId) gives idempotent upserts.
/// </summary>
public class UserCompletedCategory : IAuditable
{
    private UserCompletedCategory() { }

    private UserCompletedCategory(Guid userId, Guid categoryId)
    {
        UserId = userId;
        CategoryId = categoryId;
    }

    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }

#pragma warning disable S1144
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
#pragma warning restore S1144

    public static UserCompletedCategory Create(Guid userId, Guid categoryId)
        => new(userId, categoryId);
}
