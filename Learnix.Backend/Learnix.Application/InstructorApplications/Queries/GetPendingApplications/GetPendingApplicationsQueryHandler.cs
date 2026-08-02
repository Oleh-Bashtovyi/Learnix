using Ardalis.Specification;
using FluentResults;
using Learnix.Application.Common.Pagination;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Specifications;
using MediatR;

namespace Learnix.Application.InstructorApplications.Queries.GetPendingApplications;

internal sealed class GetPendingApplicationsQueryHandler(
    IInstructorApplicationRepository repo)
    : IRequestHandler<GetPendingApplicationsQuery, Result<PaginatedResult<PendingApplicationResponse>>>
{
    public async Task<Result<PaginatedResult<PendingApplicationResponse>>> Handle(
        GetPendingApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await repo.CountAsync(
            new PendingApplicationsCountSpecification(),
            cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<PendingApplicationResponse>.Empty(pagination.PageIndex, pagination.PageSize));

        var applications = await repo.ListAsync(
            new PendingApplicationsSpecification(pagination.Skip, pagination.Take),
            cancellationToken);

        var result = PaginatedResult<PendingApplicationResponse>.Create(
            applications.Select(a => new PendingApplicationResponse(
                a.Id,
                a.UserId,
                a.User.FirstName,
                a.User.LastName,
                a.User.Email!,
                a.MotivationText,
                a.PortfolioUrl,
                a.CreatedAt)),
            pagination.PageIndex,
            pagination.PageSize,
            totalCount);

        return Result.Ok(result);
    }
}
