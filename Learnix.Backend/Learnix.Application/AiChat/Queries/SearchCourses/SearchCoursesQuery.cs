using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.SearchCourses;

public sealed record SearchCoursesQuery(
    string Query,
    string? Category = null,
    int MaxResults = 10) : IRequest<Result<IReadOnlyList<CourseSearchResultDto>>>;
