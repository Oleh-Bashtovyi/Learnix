using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.Enrollments;

namespace Learnix.Domain.Entities;

public class Enrollment : BaseEntity
{
    private Enrollment() { }

    private Enrollment(Guid courseId, Guid studentId, decimal pricePaid)
    {
        CourseId = courseId;
        StudentId = studentId;
        PricePaid = pricePaid;
        Status = EnrollmentStatus.Active;
        PaymentStatus = pricePaid == 0m ? PaymentStatus.Completed : PaymentStatus.Pending;
        EnrolledAt = DateTime.UtcNow;
    }

    public Guid CourseId { get; private set; }
    public Guid StudentId { get; private set; }
    public Course? Course { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public decimal PricePaid { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static Enrollment Create(Guid courseId, Guid studentId, decimal pricePaid)
        => new(courseId, studentId, pricePaid);

    public void ConfirmPayment()
    {
        if (PaymentStatus == PaymentStatus.Completed)
            return;

        PaymentStatus = PaymentStatus.Completed;
    }

    public void MarkPaymentFailed()
    {
        if (PaymentStatus == PaymentStatus.Completed)
            throw new DomainException("Completed payment cannot be marked as failed.");

        PaymentStatus = PaymentStatus.Failed;
    }

    public void MarkCompleted()
    {
        if (PaymentStatus != PaymentStatus.Completed)
            throw new DomainException("Enrollment cannot be completed before payment is confirmed.");

        if (Status == EnrollmentStatus.Completed)
            return;

        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new EnrollmentCompletedDomainEvent(StudentId, CourseId));
    }
}
