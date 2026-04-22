using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Lessons.Commands.ToggleLessonVisibility;

public sealed record ToggleLessonVisibilityCommand(
    Guid CourseId,
    Guid LessonId,
    bool IsHidden) : IRequest<Result>, ICommandWithCourseId;
