using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Enrollments.Queries.GetMyEnrollments;

public sealed record GetMyEnrollmentsQuery(int Skip = 0, int Take = 20)
    : IRequest<Result<PaginatedResult<EnrolledCourseDto>>>;
