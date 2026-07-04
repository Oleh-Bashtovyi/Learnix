using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Achievements.Abstractions;

public interface IUserAchievementRepository : IRepositoryBase<UserAchievement>
{
    Task<bool> HasAchievementAsync(Guid userId, string code, CancellationToken ct);
}
