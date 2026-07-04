using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Reviews.Queries.GetCourseReviews;

public sealed record GetCourseReviewsQuery(Guid CourseId, int Skip, int Take)
    : IRequest<Result<PaginatedResult<CourseReviewDto>>>;
