namespace Learnix.Infrastructure.Outbox.Payloads.Users;

public sealed record SendUserBannedEmailPayload(string ToEmail, string FirstName);
