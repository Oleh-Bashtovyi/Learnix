using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Courses.Commands.AdminPublishCourse;

internal sealed class AdminPublishCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<AdminPublishCourseCommand, Result>
{
    public async Task<Result> Handle(AdminPublishCourseCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(CourseMessages.OnlyAdminsForcePublish));

        var course = await courseRepository.FirstOrDefaultAsync(
            new AdminCourseByIdSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CourseMessages.CourseIdNotFound(request.CourseId)));

        if (course.Status == CourseStatus.Published)
            return Result.Fail(new ConflictError(CourseMessages.CourseAlreadyPublished));

        course.Publish();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(
            cache.RemoveAsync(CacheKeys.Course(request.CourseId), cancellationToken),
            cache.RemoveAsync(CacheKeys.CoursesFeatured, cancellationToken));

        return Result.Ok();
    }
}
