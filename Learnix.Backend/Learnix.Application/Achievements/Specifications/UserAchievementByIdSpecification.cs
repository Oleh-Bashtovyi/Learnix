using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Achievements.Specifications;

public sealed class UserAchievementByIdSpecification : Specification<UserAchievement>
{
    public UserAchievementByIdSpecification(Guid userId, Guid achievementId)
    {
        Query.Where(ua => ua.UserId == userId && ua.Id == achievementId);
    }
}
