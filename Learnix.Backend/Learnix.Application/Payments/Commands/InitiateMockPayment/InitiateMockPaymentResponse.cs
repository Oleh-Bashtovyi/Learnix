namespace Learnix.Application.Payments.Commands.InitiateMockPayment;

public sealed record InitiateMockPaymentResponse(Guid PaymentId, Guid EnrollmentId);
