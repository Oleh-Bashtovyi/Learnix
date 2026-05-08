namespace Learnix.Application.Certificates.Queries.GetMyCertificates;

public sealed record MyCertificateDto(
    Guid CertificateId,
    Guid CourseId,
    string CourseTitle,
    string? CourseCoverBlobPath,
    string Code,
    DateTime IssuedAt,
    bool IsReady,
    string? DownloadUrl,
    string VerificationUrl);
