using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries;

public abstract class InstructorAnalyticsQueryHandler<TQuery, TResult>(ICurrentUserService currentUser)
    : IRequestHandler<TQuery, Result<TResult>>
    where TQuery : IRequest<Result<TResult>>
{
    public async Task<Result<TResult>> Handle(TQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        return await HandleAsync(request, currentUser.UserId.Value, cancellationToken);
    }

    protected abstract Task<Result<TResult>> HandleAsync(TQuery request, Guid instructorId, CancellationToken cancellationToken);
}
