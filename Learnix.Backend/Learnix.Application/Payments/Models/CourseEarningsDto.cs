namespace Learnix.Application.Payments.Models;

public sealed record CourseEarningsDto(
    Guid CourseId,
    string CourseTitle,
    int PaymentsCount,
    decimal TotalAmount,
    DateTime LastPaymentAt);
