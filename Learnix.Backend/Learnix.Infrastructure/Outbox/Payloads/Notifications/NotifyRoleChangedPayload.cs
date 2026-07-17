namespace Learnix.Infrastructure.Outbox.Payloads.Notifications;

/// <param name="Assigned">True when the role was granted, false when it was taken away.</param>
internal record NotifyRoleChangedPayload(Guid UserId, string Role, bool Assigned);
