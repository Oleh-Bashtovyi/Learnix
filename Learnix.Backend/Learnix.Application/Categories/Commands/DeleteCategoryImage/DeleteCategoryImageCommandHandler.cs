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

namespace Learnix.Application.Categories.Commands.DeleteCategoryImage;

internal sealed class DeleteCategoryImageCommandHandler(
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteCategoryImageCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryImageCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(CommonMessages.OnlyAdminCanManageCategories));

        var category = await categoryRepository.FirstOrDefaultAsync(
            new CategoryByIdSpecification(request.CategoryId, forUpdate: true), ct);

        if (category is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseCategoryNotFound(request.CategoryId)));

        if (category.ImageBlobPath is null)
            return Result.Fail(new NotFoundError("Category has no image."));

        await blobStorage.DeleteAsync(category.ImageBlobPath, ct);
        category.RemoveImage();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
