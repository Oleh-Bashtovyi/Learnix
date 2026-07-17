using FluentResults;
using Learnix.Application.Categories.Constants;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Categories.Commands.CreateCategory;

internal sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim();

        if (await categoryRepository.AnyAsync(new CategoryBySlugSpecification(slug), cancellationToken))
            return Result.Fail(new ConflictError(CategoryMessages.CategorySlugInUse(slug)));

        var category = Category.Create(request.Name.Trim(), slug);

        if (request.ImageBlobPath is not null)
        {
            var commitResult = await blobStorage.CommitUploadAsync(
                request.ImageBlobPath, UploadTarget.CategoryImage, cancellationToken);

            if (commitResult.IsFailed)
                return Result.Fail(commitResult.Errors);

            category.SetImage(commitResult.Value.BlobPath);
        }

        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(CacheKeys.Categories.All, cancellationToken);

        return Result.Ok(category.Id);
    }
}
