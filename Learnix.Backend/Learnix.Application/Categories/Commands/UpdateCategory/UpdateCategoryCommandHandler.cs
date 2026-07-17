using FluentResults;
using Learnix.Application.Categories.Constants;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Categories.Commands.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.FirstOrDefaultAsync(
            new CategoryByIdSpecification(request.CategoryId, forUpdate: true), cancellationToken);

        if (category is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseCategoryNotFound(request.CategoryId)));

        var slug = request.Slug.Trim();
        var existing = await categoryRepository.FirstOrDefaultAsync(new CategoryBySlugSpecification(slug), cancellationToken);

        if (existing is not null && existing.Id != request.CategoryId)
            return Result.Fail(new ConflictError(CategoryMessages.CategorySlugInUse(slug)));

        category.Rename(request.Name.Trim(), slug);

        if (request.RemoveImage)
        {
            category.SetImage(null);
        }
        else if (request.ImageBlobPath is not null && request.ImageBlobPath != category.ImageBlobPath)
        {
            var commitResult = await blobStorage.CommitUploadAsync(
                request.ImageBlobPath, UploadTarget.CategoryImage, cancellationToken);

            if (commitResult.IsFailed)
                return Result.Fail(commitResult.Errors);

            category.SetImage(commitResult.Value.BlobPath);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(CacheKeys.Categories.All, cancellationToken);

        return Result.Ok();
    }
}
