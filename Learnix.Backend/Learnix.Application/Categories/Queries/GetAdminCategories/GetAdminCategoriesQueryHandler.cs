using FluentResults;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;

namespace Learnix.Application.Categories.Queries.GetAdminCategories;

internal sealed class GetAdminCategoriesQueryHandler(
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetAdminCategoriesQuery, Result<IReadOnlyList<AdminCategoryListItemDto>>>
{
    public async Task<Result<IReadOnlyList<AdminCategoryListItemDto>>> Handle(
        GetAdminCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(new CategoriesOrderedSpecification(), cancellationToken);

        return Result.Ok<IReadOnlyList<AdminCategoryListItemDto>>(
            categories
                .Select(c => new AdminCategoryListItemDto(
                    c.Id, c.Name, c.Slug, c.IsSystem,
                    !string.IsNullOrWhiteSpace(c.ImageBlobPath) ? blobStorage.GetPublicUrl(c.ImageBlobPath) : null,
                    c.CoursesCount))
                .ToList());
    }
}
