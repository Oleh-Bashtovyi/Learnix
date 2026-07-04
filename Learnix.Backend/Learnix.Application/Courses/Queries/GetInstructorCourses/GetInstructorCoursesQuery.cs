using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetInstructorCourses;

public sealed record GetInstructorCoursesQuery(
    string? Search,
    int Skip,
    int Take,
    Guid? CategoryId) : IRequest<Result<PaginatedResult<ManageCourseCardDto>>>;
