using FluentResults;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Models;
using Learnix.Application.Payments.Services;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetInstructorEarnings;

// Admin-only: views a specific instructor's earnings, chosen from an admin screen — as opposed to
// GetMyEarnings, which always answers for the caller. Kept as two use cases rather than one branching on
// role (ADR-BACK-AUTH-018): the route decides who this is about, the handler never has to.
public sealed class GetInstructorEarningsQueryHandler(
    IUserRepository userRepository,
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetInstructorEarningsQuery, Result<InstructorEarningsResponse>>
{
    public async Task<Result<InstructorEarningsResponse>> Handle(
        GetInstructorEarningsQuery request,
        CancellationToken cancellationToken)
    {
        var instructor = await userRepository.FirstOrDefaultAsync(
            new AdminUserByIdSpecification(request.InstructorId),
            cancellationToken);

        if (instructor is null)
            return Result.Fail(new NotFoundError(CommonMessages.UserNotFoundById(request.InstructorId)));

        return Result.Ok(await InstructorEarnings.ComputeAsync(
            paymentRepository, request.InstructorId, cancellationToken));
    }
}
