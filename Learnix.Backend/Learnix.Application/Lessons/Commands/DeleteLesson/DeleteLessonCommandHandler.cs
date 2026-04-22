using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Lessons.Specification;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.Lessons.Commands.DeleteLesson;

internal sealed class DeleteLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteLessonCommand, Result>
{
    public async Task<Result> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, forUpdate: true), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError($"Course {request.CourseId} not found."));

        if (course.InstructorId != currentUser.UserId && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("You are not the owner of this course."));

        var lesson = await lessonRepository.FirstOrDefaultAsync(new LessonByIdSpecification(request.LessonId, forUpdate: true));

        if (lesson is null)
            return Result.Fail(new NotFoundError($"Lesson {request.LessonId} not found."));

        await lessonRepository.DeleteAsync(lesson);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Ok();
    }
}