using FluentResults;
using Learnix.Application.AiChat.Specifications;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;

namespace Learnix.Application.AiChat.Queries.SearchCourses;

internal sealed class SearchCoursesQueryHandler(
    ICourseRepository courseRepository,
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

        var spec = new CourseSearchSpecification(request.Query, categoryId, maxResults);
        var courses = await courseRepository.ListAsync(spec, cancellationToken);

        var categoryIds = courses.Select(c => c.CategoryId).Distinct().ToList();
        var categories = await categoryRepository.ListAsync(
            new CategoriesByIdsSpecification(categoryIds), cancellationToken);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        var results = courses
            .Select(c => new CourseSearchResultDto(
                c.Id,
                c.Title,
                c.Description.Length > 200 ? c.Description[..200] + "..." : c.Description,
                categoryMap.TryGetValue(c.CategoryId, out var name) ? name : "Unknown",
                c.Price,
                c.Price == 0,
                c.EnrollmentsCount))
            .ToList();

        return Result.Ok<IReadOnlyList<CourseSearchResultDto>>(results);
    }
}
