using FluentResults;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Queries.GetInstructorCourses;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetAdminCourses;

public sealed record GetAdminCoursesQuery(
    string? Search,
    int Skip,
    int Take,
    Guid? CategoryId,
    bool IncludeDeleted = false) : IRequest<Result<PaginatedResult<ManageCourseCardDto>>>;
