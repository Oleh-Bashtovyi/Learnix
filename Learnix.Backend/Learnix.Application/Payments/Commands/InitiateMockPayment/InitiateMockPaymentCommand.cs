using FluentResults;
using MediatR;

namespace Learnix.Application.Payments.Commands.InitiateMockPayment;

public sealed record InitiateMockPaymentCommand(Guid CourseId)
    : IRequest<Result<InitiateMockPaymentResponse>>;
