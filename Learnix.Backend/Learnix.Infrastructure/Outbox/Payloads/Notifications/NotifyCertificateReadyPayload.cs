namespace Learnix.Infrastructure.Outbox.Payloads.Notifications;

internal record NotifyCertificateReadyPayload(Guid UserId, Guid CertificateId, string CourseTitle);
