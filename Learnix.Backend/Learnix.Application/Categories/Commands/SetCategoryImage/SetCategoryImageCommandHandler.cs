using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Categories.Commands.SetCategoryImage;

internal sealed class SetCategoryImageCommandHandler(
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDistributedCache cache)
    : IRequestHandler<SetCategoryImageCommand, Result>
{
    public async Task<Result> Handle(SetCategoryImageCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(CommonMessages.OnlyAdminCanManageCategories));

        var commitResult = await blobStorage.CommitUploadAsync(request.BlobPath, UploadTarget.CategoryImage, ct);
        if (commitResult.IsFailed)
            return Result.Fail(commitResult.Errors);

        var category = await categoryRepository.FirstOrDefaultAsync(
            new CategoryByIdSpecification(request.CategoryId, forUpdate: true), ct);

        if (category is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseCategoryNotFound(request.CategoryId)));

        category.SetImage(commitResult.Value.BlobPath);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.CategoriesAll, ct);

        return Result.Ok();
    }
}
