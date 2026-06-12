namespace Learnix.Application.Certificates.Queries.VerifyCertificate;

public sealed record VerifyCertificateResponse(
    string Code,
    string CourseTitle,
    string StudentFirstName,
    string StudentLastName,
    string InstructorFirstName,
    string InstructorLastName,
    DateTime IssuedAt,
    bool IsReady,
    string? DownloadUrl);
