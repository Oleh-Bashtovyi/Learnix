namespace Learnix.Infrastructure.Outbox.Payloads.Users;

public sealed record SendUserRoleChangedEmailPayload(string ToEmail, string FirstName, string Role, bool Assigned, string Language);
