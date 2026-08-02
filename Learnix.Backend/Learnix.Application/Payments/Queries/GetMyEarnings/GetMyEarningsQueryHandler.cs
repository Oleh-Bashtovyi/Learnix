using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Models;
using Learnix.Application.Payments.Services;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetMyEarnings;

public sealed class GetMyEarningsQueryHandler(
    ICurrentUserService currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetMyEarningsQuery, Result<InstructorEarningsResponse>>
{
    public async Task<Result<InstructorEarningsResponse>> Handle(
        GetMyEarningsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        return Result.Ok(await InstructorEarnings.ComputeAsync(
            paymentRepository, currentUser.UserId.Value, cancellationToken));
    }
}
