using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Courses.Commands.UpdateCourseDetails;

public sealed class UpdateCourseDetailsCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ICategoryRepository categoryRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : CourseCommandHandler<UpdateCourseDetailsCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UpdateCourseDetailsCommand request, Course course, CancellationToken cancellationToken)
    {
        var categoryExists = await categoryRepository.AnyAsync(
            new CategoryByIdSpecification(request.CategoryId), cancellationToken);
        if (!categoryExists)
            return Result.Fail(new NotFoundError(CommonMessages.CourseCategoryNotFound(request.CategoryId)));

        //
        // SAME AS IN CREATE COURSE. MOVE TO UTILS???
        //
        var normalizedTags = request.Tags
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .DistinctBy(t => t.ToLowerInvariant())
            .ToList();

        course.UpdateDetails(
            request.CategoryId,
            request.Title.Trim(),
            request.Description,
            request.Price,
            normalizedTags);

        if (request.CoverImageUrl is not null && request.CoverImageUrl != course.CoverBlobPath)
        {
            var commitResult = await blobStorage.CommitUploadAsync(
                request.CoverImageUrl, UploadTarget.CourseCover, cancellationToken);

            if (commitResult.IsFailed)
                return Result.Fail(commitResult.Errors);

            course.SetCoverImage(commitResult.Value.BlobPath);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(
            cache.RemoveAsync(CacheKeys.Courses.ById(request.CourseId), cancellationToken),
            cache.RemoveAsync(CacheKeys.Courses.Featured, cancellationToken));

        return Result.Ok();
    }
}
