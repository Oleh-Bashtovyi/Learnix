namespace Learnix.Application.Certificates.Abstractions;

public interface ICertificateNotifier
{
    Task NotifyCertificateReadyAsync(Guid userId, Guid certificateId, string courseTitle, CancellationToken ct);
}
