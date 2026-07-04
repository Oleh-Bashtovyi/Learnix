using Ardalis.Specification;
using FluentResults;
using Learnix.Application.AiChat.Specifications;
using Learnix.Application.Courses.Abstractions;
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
            new AllCategoriesSpecification(), cancellationToken);

        var result = categories
            .Select(c => new CategoryAiDto(c.Name, c.Slug, c.CoursesCount))
            .ToList();

        return Result.Ok<IReadOnlyList<CategoryAiDto>>(result);
    }
}
