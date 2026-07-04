using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Courses.Commands.PublishCourse;

public sealed class PublishCourseCommandHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : CourseCommandHandler<PublishCourseCommand, Result>(courseRepository, currentUser, includeLessons: true)
{
    protected override async Task<Result> HandleAsync(
        PublishCourseCommand request, Course course, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(course.CoverBlobPath))
            return Result.Fail(new ConflictError(CourseMessages.CannotPublishNoCoverImage));

        if (course.Sections.Count == 0)
            return Result.Fail(new ConflictError(CourseMessages.CannotPublishNoSection));

        if (course.Sections.All(s => s.Lessons.Count == 0))
            return Result.Fail(new ConflictError(CourseMessages.CannotPublishNoLesson));

        course.Publish();

        await unitOfWork.SaveChangesAsync(ct);

        await Task.WhenAll(
            cache.RemoveAsync(CacheKeys.Course(request.CourseId), ct),
            cache.RemoveAsync(CacheKeys.CoursesFeatured, ct));

        return Result.Ok();
    }
}
