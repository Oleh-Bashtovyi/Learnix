using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;

namespace Learnix.Application.AiChat.Queries.SearchCourses;

internal sealed class SearchCoursesQueryHandler(
    IAiCourseSearchService searchService,
    ICategoryRepository categoryRepository)
    : IRequestHandler<SearchCoursesQuery, Result<IReadOnlyList<CourseSearchResultDto>>>
{
    public async Task<Result<IReadOnlyList<CourseSearchResultDto>>> Handle(
        SearchCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);

        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = await categoryRepository.FirstOrDefaultAsync(
                new CategoryBySlugSpecification(request.Category),
                cancellationToken);
            categoryId = category?.Id;
        }

        var results = await searchService.SearchAsync(request.Query, categoryId, maxResults, cancellationToken);

        return Result.Ok(results);
    }
}
