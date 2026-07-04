namespace Learnix.Infrastructure.Outbox.Payloads.Users;

public sealed record SendUserUnbannedEmailPayload(string ToEmail, string FirstName, string Language);
