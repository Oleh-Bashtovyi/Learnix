namespace Learnix.Infrastructure.Outbox.Payloads.Users;

public record SendPasswordResetEmailPayload(
    string ToEmail,
    string FirstName,
    string ResetLink,
    string Language);
