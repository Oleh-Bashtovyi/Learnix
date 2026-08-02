using System.Text.Json;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using Learnix.Infrastructure.Outbox.Handlers.Notifications;
using Learnix.Infrastructure.Outbox.Payloads.Notifications;

namespace Learnix.Infrastructure.UnitTests.Outbox;

/// <summary>
/// One boolean decides whether the user is told they gained a role or lost one. Inverting it is a silent
/// bug: both branches compile, both send a notification, and only the reader notices.
/// <para>
/// Driven through the JSON entry point the dispatcher actually calls, so the payload has to survive a
/// round-trip to count as handled.
/// </para>
/// </summary>
public class RoleChangedNotificationHandlerTests
{
    private readonly INotificationSender _sender = Substitute.For<INotificationSender>();
    private readonly RoleChangedNotificationHandler _sut;

    public RoleChangedNotificationHandlerTests()
    {
        _sut = new RoleChangedNotificationHandler(_sender);
    }

    private Task HandleAsync(NotifyRoleChangedPayload payload) =>
        _sut.HandleAsync(JsonSerializer.Serialize(payload), CancellationToken.None);

    [Fact]
    public async Task HandleAsync_WhenRoleWasAssigned_ShouldSendRoleAssignedCarryingTheRole()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await HandleAsync(new NotifyRoleChangedPayload(userId, Roles.Instructor, Assigned: true));

        // Assert
        await _sender.Received(1).SendAsync(
            userId,
            NotificationType.RoleAssigned,
            Arg.Is<IReadOnlyDictionary<string, string>>(p => p["role"] == Roles.Instructor),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRoleWasRemoved_ShouldSendRoleRemovedCarryingTheRole()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await HandleAsync(new NotifyRoleChangedPayload(userId, Roles.Instructor, Assigned: false));

        // Assert
        await _sender.Received(1).SendAsync(
            userId,
            NotificationType.RoleRemoved,
            Arg.Is<IReadOnlyDictionary<string, string>>(p => p["role"] == Roles.Instructor),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The handler must stay blind to which role it carries — that is what lets Admin work without a
    /// branch of its own (ADR-BACK-NOTIF-002).
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenTheRoleIsAdmin_ShouldSendTheSameWayAsAnyOtherRole()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await HandleAsync(new NotifyRoleChangedPayload(userId, Roles.Admin, Assigned: false));

        // Assert
        await _sender.Received(1).SendAsync(
            userId,
            NotificationType.RoleRemoved,
            Arg.Is<IReadOnlyDictionary<string, string>>(p => p["role"] == Roles.Admin),
            Arg.Any<CancellationToken>());
    }
}
