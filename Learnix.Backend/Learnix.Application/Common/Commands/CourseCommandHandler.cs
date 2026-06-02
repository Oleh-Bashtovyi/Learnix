using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Extensions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.Common.Commands;

public abstract class CourseCommandHandler<TCommand, TResult>(
    ICourseRepository courseRepository,
    ICurrentUserService currentUser,
    bool includeLessons = false)
    : IRequestHandler<TCommand, TResult>
    where TCommand : ICommandWithCourseId, IRequest<TResult>
    where TResult : ResultBase, new()
{
    public async Task<TResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true, includeLessons: includeLessons, forUpdate: true), cancellationToken);

        if (course is null)
            return Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (!course.IsOwnerOrAdmin(currentUser))
            return Fail(new ForbiddenError(CommonMessages.NotOwnerOfCourse));

        course.EnsureStructureMutable();

        return await HandleAsync(request, course, cancellationToken);
    }

    protected abstract Task<TResult> HandleAsync(TCommand request, Course course, CancellationToken ct);

    protected static TResult Fail(IError error)
    {
        var result = new TResult();
        result.Reasons.Add(error);
        return result;
    }
}

public abstract class CourseSectionCommandHandler<TCommand, TResult>(
    ICourseRepository courseRepository,
    ICurrentUserService currentUser,
    bool lessonsBySectionId = false)
    : IRequestHandler<TCommand, TResult>
    where TCommand : ICommandWithCourseAndSectionId, IRequest<TResult>
    where TResult : ResultBase, new()
{
    public async Task<TResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true, sectionIdForLessons: lessonsBySectionId ? request.SectionId : null, forUpdate: true), cancellationToken);

        if (course is null)
            return Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (!course.IsOwnerOrAdmin(currentUser))
            return Fail(new ForbiddenError(CommonMessages.NotOwnerOfCourse));

        if (!course.SectionExists(request.SectionId))
            return Fail(new NotFoundError(CommonMessages.SectionNotFound(request.SectionId)));

        course.EnsureStructureMutable();

        return await HandleAsync(request, course, cancellationToken);
    }

    protected abstract Task<TResult> HandleAsync(TCommand request, Course course, CancellationToken ct);

    protected static TResult Fail(IError error)
    {
        var result = new TResult();
        result.Reasons.Add(error);
        return result;
    }
}
