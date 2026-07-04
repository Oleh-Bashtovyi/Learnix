namespace Learnix.Application.Certificates.Abstractions;

public sealed record CertificateDocumentData(
    string StudentFullName,
    string CourseTitle,
    string InstructorName,
    DateTime CompletedAt,
    string Code,
    string VerificationUrl);

public interface ICertificatePdfGenerator
{
    byte[] Generate(CertificateDocumentData data);
}
