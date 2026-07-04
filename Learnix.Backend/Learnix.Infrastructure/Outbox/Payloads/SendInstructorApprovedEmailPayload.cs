namespace Learnix.Infrastructure.Outbox.Payloads;

internal record SendInstructorApprovedEmailPayload(string ToEmail, string FirstName, string Language);
