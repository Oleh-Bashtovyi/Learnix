using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;

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

    public Course? Course { get; private set; }
    public string Code { get; private set; } = null!;
    public string? FileUrl { get; private set; }
    public DateTime IssuedAt { get; private set; }

    public static Certificate Create(Guid courseId, Guid studentId, Guid enrollmentId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Certificate code cannot be empty.");

        return new Certificate(courseId, studentId, enrollmentId, code);
    }

    public void AttachFile(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new DomainException("Certificate file URL cannot be empty.");

        FileUrl = fileUrl;
    }
}
