using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.InstructorApplications.Queries.GetPendingApplications;

public record GetPendingApplicationsQuery(int Skip = 0, int Take = 20)
    : IRequest<Result<PaginatedResult<PendingApplicationResponse>>>;
