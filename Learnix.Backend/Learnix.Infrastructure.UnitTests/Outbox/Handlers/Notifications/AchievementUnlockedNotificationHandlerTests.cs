using System.Text.Json;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Outbox.Handlers.Notifications;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;

namespace Learnix.Infrastructure.UnitTests.Outbox.Handlers.Notifications;

/// <summary>
/// Unlocking an achievement fans out to two independent collaborators — the achievement record it marks as
/// "seen" and the bell notification a client renders — and this handler is the only place that keeps them
/// in sync. Losing either call means either a badge nobody hears about or a notification for nothing.
/// </summary>
public class AchievementUnlockedNotificationHandlerTests
{
    private readonly IAchievementNotifier _achievementNotifier = Substitute.For<IAchievementNotifier>();
    private readonly INotificationSender _notificationSender = Substitute.For<INotificationSender>();
    private readonly AchievementUnlockedNotificationHandler _sut;

    public AchievementUnlockedNotificationHandlerTests()
    {
        _sut = new AchievementUnlockedNotificationHandler(_achievementNotifier, _notificationSender);
    }

    [Fact]
    public async Task HandleAsync_NotifiesTheAchievementRecord_WithEveryFieldFromThePayload()
    {
        var payload = new NotifyAchievementUnlockedPayload(
            Guid.NewGuid(), Guid.NewGuid(), "first-course-completed", DateTime.UtcNow);

        await HandleAsync(payload);

        await _achievementNotifier.Received(1).NotifyAsync(
            payload.UserId, payload.UserAchievementId, payload.Code, payload.UnlockedAt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SendsTheBellNotification_CarryingTheAchievementCodeButNotAPrettifiedName()
    {
        // The client already has a name for every achievement code; sending anything else here would be a
        // second, driftable copy of that name living in the outbox.
        var payload = new NotifyAchievementUnlockedPayload(
            Guid.NewGuid(), Guid.NewGuid(), "first-course-completed", DateTime.UtcNow);

        await HandleAsync(payload);

        await _notificationSender.Received(1).SendAsync(
            payload.UserId,
            NotificationType.AchievementEarned,
            Arg.Is<IReadOnlyDictionary<string, string>>(p => p["code"] == "first-course-completed"),
            Arg.Any<CancellationToken>());
    }

    private Task HandleAsync(NotifyAchievementUnlockedPayload payload) =>
        _sut.HandleAsync(JsonSerializer.Serialize(payload), CancellationToken.None);
}
