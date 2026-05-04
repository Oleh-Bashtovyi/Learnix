using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Categories.Queries.GetAdminCategories;

internal sealed class GetAdminCategoriesQueryHandler(
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAdminCategoriesQuery, Result<IReadOnlyList<AdminCategoryListItemDto>>>
{
    public async Task<Result<IReadOnlyList<AdminCategoryListItemDto>>> Handle(
        GetAdminCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(CommonMessages.OnlyAdminCanManageCategories));

        var categories = await categoryRepository.ListAsync(new CategoriesOrderedSpecification(), cancellationToken);

        return Result.Ok<IReadOnlyList<AdminCategoryListItemDto>>(
            categories
                .Select(c => new AdminCategoryListItemDto(
                    c.Id, c.Name, c.Slug, c.IsSystem,
                    c.ImageBlobPath is not null
                        ? blobStorage.GenerateReadUrl(c.ImageBlobPath, TimeSpan.FromHours(24))
                        : null))
                .ToList());
    }
}
