using FluentResults;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Courses.Commands.AdminDeleteCourse;

internal sealed class AdminDeleteCourseCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    IDistributedCache cache)
    : IRequestHandler<AdminDeleteCourseCommand, Result>
{
    public async Task<Result> Handle(AdminDeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new AdminCourseByIdSpecification(request.CourseId, forUpdate: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (course.IsDeleted)
            return Result.Fail(new ConflictError(CourseMessages.CourseAlreadyDeleted));

        course.AdminDelete();
        await courseRepository.DeleteAsync(course, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(
            cache.RemoveAsync(CacheKeys.Courses.ById(request.CourseId), cancellationToken),
            cache.RemoveAsync(CacheKeys.Courses.Featured, cancellationToken));

        return Result.Ok();
    }
}
