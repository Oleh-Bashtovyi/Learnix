namespace Learnix.Application.Payments.Queries.GetAdminPayments;

public sealed record AdminPaymentDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    Guid CourseId,
    string CourseTitle,
    decimal Amount,
    string Currency,
    string Status,
    string PaymentProvider,
    DateTime CreatedAt,
    DateTime? CompletedAt);
