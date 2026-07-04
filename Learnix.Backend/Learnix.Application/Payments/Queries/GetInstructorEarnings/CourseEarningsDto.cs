namespace Learnix.Application.Payments.Queries.GetInstructorEarnings;

public sealed record CourseEarningsDto(
    Guid CourseId,
    string CourseTitle,
    int PaymentsCount,
    decimal TotalAmount,
    DateTime LastPaymentAt);
