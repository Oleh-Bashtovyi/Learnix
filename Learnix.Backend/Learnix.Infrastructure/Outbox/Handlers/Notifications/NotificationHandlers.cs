using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Outbox.Payloads.Achievements;
using Learnix.Infrastructure.Outbox.Payloads.Notifications;

namespace Learnix.Infrastructure.Outbox.Handlers.Notifications;

/// <summary>
/// In-app notifications: a stored row the bell reads, plus a live push over SignalR for whoever is online.
/// <para>
/// These handlers decide <b>what happened</b> and hand over the facts it happened to. They never write a
/// sentence: the client renders the type through its own i18n, the way it renders every other string it shows
/// (ADR-BACK-NOTIF-001). That is also why a title in the wrong language can no longer be stored forever.
/// </para>
/// </summary>
internal sealed class AchievementUnlockedNotificationHandler(
    IAchievementNotifier achievementNotifier,
    INotificationSender notificationSender)
    : OutboxMessageHandler<NotifyAchievementUnlockedPayload>
{
    public override string MessageType => OutboxMessageTypes.NotifyAchievementUnlocked;

    protected override async Task HandleAsync(NotifyAchievementUnlockedPayload payload, CancellationToken cancellationToken)
    {
        await achievementNotifier.NotifyAsync(
            payload.UserId, payload.UserAchievementId, payload.Code, payload.UnlockedAt, cancellationToken);

        // The code, not a prettified version of it: the client has a name for every achievement already.
        await notificationSender.SendAsync(
            payload.UserId,
            NotificationType.AchievementEarned,
            new Dictionary<string, string> { ["code"] = payload.Code },
            cancellationToken);
    }
}

internal sealed class CertificateIssuedNotificationHandler(
    ICertificateNotifier certificateNotifier,
    INotificationSender notificationSender)
    : OutboxMessageHandler<NotifyCertificateIssuedPayload>
{
    public override string MessageType => OutboxMessageTypes.NotifyCertificateIssued;

    protected override async Task HandleAsync(NotifyCertificateIssuedPayload payload, CancellationToken cancellationToken)
    {
        await certificateNotifier.NotifyCertificateIssuedAsync(
            payload.UserId, payload.CertificateId, payload.CourseId, payload.CourseTitle, cancellationToken);

        await notificationSender.SendAsync(
            payload.UserId,
            NotificationType.CertificateReady,
            new Dictionary<string, string> { ["courseTitle"] = payload.CourseTitle },
            cancellationToken);
    }
}

/// <summary>The type is the whole message — there is nothing to fill in.</summary>
internal sealed class InstructorApprovedNotificationHandler(INotificationSender notificationSender)
    : OutboxMessageHandler<NotifyInstructorApprovedPayload>
{
    public override string MessageType => OutboxMessageTypes.NotifyInstructorApproved;

    protected override Task HandleAsync(NotifyInstructorApprovedPayload payload, CancellationToken cancellationToken) =>
        notificationSender.SendAsync(payload.UserId, NotificationType.InstructorApproved, cancellationToken: cancellationToken);
}

internal sealed class InstructorRejectedNotificationHandler(INotificationSender notificationSender)
    : OutboxMessageHandler<NotifyInstructorRejectedPayload>
{
    public override string MessageType => OutboxMessageTypes.NotifyInstructorRejected;

    protected override Task HandleAsync(NotifyInstructorRejectedPayload payload, CancellationToken cancellationToken) =>
        notificationSender.SendAsync(payload.UserId, NotificationType.InstructorRejected, cancellationToken: cancellationToken);
}

/// <summary>
/// An admin granting or revoking a role by hand — the path that bypasses the application flow, so
/// <see cref="NotificationType.InstructorApproved"/> would be a lie here: nobody approved anything.
/// <para>
/// One handler covers every role because the role travels as a parameter rather than as a type. Losing a
/// role is the half that matters most: an instructor whose dashboard disappears is owed a reason, and
/// this is the only place the platform gives one.
/// </para>
/// </summary>
internal sealed class RoleChangedNotificationHandler(INotificationSender notificationSender)
    : OutboxMessageHandler<NotifyRoleChangedPayload>
{
    public override string MessageType => OutboxMessageTypes.NotifyRoleChanged;

    protected override Task HandleAsync(NotifyRoleChangedPayload payload, CancellationToken cancellationToken) =>
        notificationSender.SendAsync(
            payload.UserId,
            payload.Assigned ? NotificationType.RoleAssigned : NotificationType.RoleRemoved,
            new Dictionary<string, string> { ["role"] = payload.Role },
            cancellationToken);
}
