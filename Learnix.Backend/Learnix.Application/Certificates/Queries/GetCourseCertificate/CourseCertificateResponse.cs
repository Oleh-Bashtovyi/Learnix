namespace Learnix.Application.Certificates.Queries.GetCourseCertificate;

public sealed record CourseCertificateResponse(
    Guid CertificateId,
    string Code,
    DateTime IssuedAt,
    bool IsReady,
    string? DownloadUrl,
    string VerificationUrl);
