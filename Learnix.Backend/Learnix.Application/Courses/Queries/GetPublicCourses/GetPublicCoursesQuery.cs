using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPublicCourses;

public sealed record GetPublicCoursesQuery(
    string? Search,
    int Skip,
    int Take,
    Guid? CategoryId,
    Guid? InstructorId,
    string? SortBy,
    bool? IsFree,
    decimal? MinRating) : IRequest<Result<PaginatedResult<PublicCourseCardDto>>>;
