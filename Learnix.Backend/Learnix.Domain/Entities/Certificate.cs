using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.Certificates;

namespace Learnix.Domain.Entities;

public class Certificate : BaseEntity
{
    private Certificate() { }

    private Certificate(Guid courseId, Guid studentId, Guid enrollmentId, string code)
    {
        CourseId = courseId;
        StudentId = studentId;
        EnrollmentId = enrollmentId;
        Code = code;
        IssuedAt = DateTime.UtcNow;
    }

    public Guid CourseId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid EnrollmentId { get; private set; }

    // S1144: no code calls the setter — EF Core materializes the navigation.
#pragma warning disable S1144
    public Course? Course { get; private set; }
#pragma warning restore S1144

    public string Code { get; private set; } = null!;
    public string? FilePath { get; private set; }
    public DateTime IssuedAt { get; private set; }

    /// <summary>
    /// A certificate is proof of a completed enrollment, so the enrollment is what it is issued
    /// from: every id and the completion itself come from one consistent source. The course is
    /// passed alongside — rather than read off <c>enrollment.Course</c>, which is only populated if
    /// the query happened to include it — so the issued event can name the course without a second
    /// lookup. It must be the very course the student enrolled in.
    /// </summary>
    public static Certificate Issue(Enrollment enrollment, Course course)
    {
        if (enrollment.Status != EnrollmentStatus.Completed)
            throw new DomainException("Certificate cannot be issued for an enrollment that is not completed.");

        if (course.Id != enrollment.CourseId)
            throw new DomainException("Certificate course does not match the enrolled course.");

        var certificate = new Certificate(
            enrollment.CourseId,
            enrollment.StudentId,
            enrollment.Id,
            GenerateCode());

        certificate.RaiseDomainEvent(new CertificateIssuedDomainEvent(
            certificate.Id,
            certificate.StudentId,
            course.Id,
            course.Title));

        return certificate;
    }

    /// <summary>
    /// Replaces the rendered PDF. Regenerating a certificate writes to the same deterministic blob
    /// path, overwriting the file in place — so only a genuinely different path orphans the old blob
    /// and needs an event to reap it. Raising one unconditionally would enqueue a delete for the
    /// file that was just uploaded.
    /// </summary>
    public void AttachFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new DomainException("Certificate file path cannot be empty.");

        if (FilePath == filePath)
            return;

        if (FilePath is not null)
            RaiseDomainEvent(new CertificateFileReplacedDomainEvent(Id, FilePath));

        FilePath = filePath;
    }

    private static string GenerateCode()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"CERT-{date}-{random}";
    }
}
