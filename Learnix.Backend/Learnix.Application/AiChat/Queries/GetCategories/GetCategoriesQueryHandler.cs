using FluentResults;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetCategories;

internal sealed class GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryAiDto>>>
{
    public async Task<Result<IReadOnlyList<CategoryAiDto>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(
            new CategoriesOrderedSpecification(), cancellationToken);

        // The AI recommends by popularity, unlike the catalog's alphabetical browsing order.
        var result = categories
            .OrderByDescending(c => c.CoursesCount)
            .Select(c => new CategoryAiDto(c.Name, c.Slug, c.CoursesCount))
            .ToList();

        return Result.Ok<IReadOnlyList<CategoryAiDto>>(result);
    }
}
