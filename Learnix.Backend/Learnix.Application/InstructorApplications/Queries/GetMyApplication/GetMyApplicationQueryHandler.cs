using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Specifications;
using MediatR;

namespace Learnix.Application.InstructorApplications.Queries.GetMyApplication;

internal sealed class GetMyApplicationQueryHandler(
    IInstructorApplicationRepository repo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyApplicationQuery, Result<MyApplicationResponse?>>
{
    public async Task<Result<MyApplicationResponse?>> Handle(
        GetMyApplicationQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var application = await repo.FirstOrDefaultAsync(
            new ApplicationByUserIdSpecification(currentUser.UserId.Value),
            cancellationToken);

        if (application is null)
            return Result.Ok<MyApplicationResponse?>(null);

        return Result.Ok<MyApplicationResponse?>(new MyApplicationResponse(
            application.Id,
            application.Status.ToString(),
            application.MotivationText,
            application.PortfolioUrl,
            application.RejectionReason,
            application.CreatedAt,
            application.ReviewedAt));
    }
}
