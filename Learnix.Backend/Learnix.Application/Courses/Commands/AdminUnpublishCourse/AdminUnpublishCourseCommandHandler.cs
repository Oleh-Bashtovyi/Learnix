using FluentResults;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Courses.Commands.AdminUnpublishCourse;

internal sealed class AdminUnpublishCourseCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<AdminUnpublishCourseCommand, Result>
{
    public async Task<Result> Handle(AdminUnpublishCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new AdminCourseByIdSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (course.Status != CourseStatus.Published)
            return Result.Fail(new ConflictError(CourseMessages.CourseNotPublished));

        course.AdminUnpublish();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(
            cache.RemoveAsync(CacheKeys.Courses.ById(request.CourseId), cancellationToken),
            cache.RemoveAsync(CacheKeys.Courses.Featured, cancellationToken),
            cache.RemoveAsync(CacheKeys.Courses.PublishedCount, cancellationToken));

        return Result.Ok();
    }
}
