using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Courses.Commands.UnpublishCourse;

public sealed class UnpublishCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : CourseCommandHandler<UnpublishCourseCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        UnpublishCourseCommand request, Course course, CancellationToken ct)
    {
        course.Unpublish();

        await unitOfWork.SaveChangesAsync(ct);

        await Task.WhenAll(
            cache.RemoveAsync(CacheKeys.Course(request.CourseId), ct),
            cache.RemoveAsync(CacheKeys.CoursesFeatured, ct));

        return Result.Ok();
    }
}
