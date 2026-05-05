using Learnix.Application.Achievements.Abstractions;
using Learnix.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.Infrastructure.Services.Achievements;

internal sealed class SignalRAchievementNotifier(
    IHubContext<AchievementsHub, IAchievementsHubClient> hubContext)
    : IAchievementNotifier
{
    public Task NotifyAsync(Guid userId, Guid achievementId, string code, DateTime unlockedAt, CancellationToken ct)
        => hubContext
            .Clients
            .Group(AchievementsHub.UserGroup(userId.ToString()))
            .AchievementUnlocked(new AchievementUnlockedNotification(achievementId, code, unlockedAt));
}
