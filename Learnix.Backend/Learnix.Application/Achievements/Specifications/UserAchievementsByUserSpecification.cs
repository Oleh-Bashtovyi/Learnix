using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Achievements.Specifications;

public sealed class UserAchievementsByUserSpecification : Specification<UserAchievement>
{
    public UserAchievementsByUserSpecification(Guid userId)
    {
        Query
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.UnlockedAt);
    }
}
