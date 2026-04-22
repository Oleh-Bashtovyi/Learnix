using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Lessons.Commands.UpdatePostLesson;

public sealed record UpdatePostLessonCommand(
    Guid CourseId,
    Guid LessonId,
    string Title,
    string Content) : IRequest<Result>, ICommandWithCourseId;
