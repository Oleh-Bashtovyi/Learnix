using Learnix.API.Hubs;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Common.Abstractions.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.API.Services.Notifications;

internal sealed class SignalRAchievementNotifier(
    IHubContext<NotificationsHub, INotificationsHubClient> hubContext)
    : IAchievementNotifier
{
    public Task NotifyAsync(Guid userId, Guid achievementId, string code, DateTime unlockedAt, CancellationToken ct)
        => hubContext
            .Clients
            .Group(NotificationsHub.UserGroup(userId.ToString()))
            .AchievementUnlocked(new AchievementUnlockedNotification(achievementId, code, unlockedAt));
}
