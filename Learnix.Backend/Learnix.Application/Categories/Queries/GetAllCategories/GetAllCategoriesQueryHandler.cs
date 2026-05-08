using FluentResults;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;

namespace Learnix.Application.Categories.Queries.GetAllCategories;

internal sealed class GetAllCategoriesQueryHandler(
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetAllCategoriesQuery, Result<IReadOnlyList<CategoryListItemDto>>>
{
    public async Task<Result<IReadOnlyList<CategoryListItemDto>>> Handle(
        GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(new CategoriesOrderedSpecification(), cancellationToken);

        return Result.Ok<IReadOnlyList<CategoryListItemDto>>(
            categories
                .Select(c => new CategoryListItemDto(
                    c.Id, c.Name, c.Slug,
                    c.ImageBlobPath is not null
                        ? blobStorage.GenerateReadUrl(c.ImageBlobPath, TimeSpan.FromHours(24))
                        : null,
                    c.CoursesCount))
                .ToList());
    }
}
