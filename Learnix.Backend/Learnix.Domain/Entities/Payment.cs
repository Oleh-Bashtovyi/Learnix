using Learnix.Domain.Common;
using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public class Payment : BaseEntity
{
    private Payment() { }

    private Payment(Guid userId, Guid courseId, Guid enrollmentId, decimal amount)
    {
        UserId = userId;
        CourseId = courseId;
        EnrollmentId = enrollmentId;
        Amount = amount;
        Currency = "USD";
        Status = PaymentStatus.Completed;
        PaymentProvider = "Mock";
        CompletedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string PaymentProvider { get; private set; } = null!;
    public DateTime? CompletedAt { get; private set; }

    // S1144: no code calls these setters — EF Core materializes the navigations.
#pragma warning disable S1144
    public Course? Course { get; private set; }
    public User? User { get; private set; }
    public Enrollment? Enrollment { get; private set; }
#pragma warning restore S1144

    public static Payment CreateMock(Guid userId, Guid courseId, Guid enrollmentId, decimal amount)
        => new(userId, courseId, enrollmentId, amount);
}
