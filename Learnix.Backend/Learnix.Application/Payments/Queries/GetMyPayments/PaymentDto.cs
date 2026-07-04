namespace Learnix.Application.Payments.Queries.GetMyPayments;

public sealed record PaymentDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    decimal Amount,
    string Currency,
    string Status,
    string PaymentProvider,
    DateTime CreatedAt,
    DateTime? CompletedAt);
