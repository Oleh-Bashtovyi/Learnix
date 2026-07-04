namespace Learnix.Infrastructure.Outbox.Payloads;

internal record SendInstructorRejectedEmailPayload(string ToEmail, string FirstName, string? RejectionReason, string Language);
