using FluentResults;
using Learnix.Application.Categories.Constants;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Categories.Commands.DeleteCategory;

internal sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDistributedCache cache)
    : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(CommonMessages.OnlyAdminCanManageCategories));

        var category = await categoryRepository.FirstOrDefaultAsync(
            new CategoryByIdSpecification(request.CategoryId, forUpdate: true), cancellationToken);

        if (category is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseCategoryNotFound(request.CategoryId)));

        if (category.IsSystem)
            return Result.Fail(new ConflictError(CategoryMessages.SystemCategoriesCannotBeDeleted));

        if (await courseRepository.AnyAsync(new CourseByCategoryIdSpecification(request.CategoryId), cancellationToken))
            return Result.Fail(new ConflictError(CategoryMessages.CategoryUsedByCourses));

        await categoryRepository.DeleteAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(CacheKeys.CategoriesAll, cancellationToken);

        return Result.Ok();
    }
}
