using Learnix.Domain.Entities;

namespace Learnix.Application.Achievements.Abstractions;

public interface IUserAchievementProgressRepository
{
    Task<UserAchievementProgress?> GetAsync(Guid userId, CancellationToken ct);
    Task<UserAchievementProgress> GetOrCreateAsync(Guid userId, CancellationToken ct);
}
