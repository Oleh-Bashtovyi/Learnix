using FluentResults;
using MediatR;

namespace Learnix.Application.Lessons.Commands.ToggleLessonVisibility;

public sealed record ToggleLessonVisibilityCommand(
    Guid CourseId,
    Guid Lesson,
    bool isHidden) : IRequest<Result>;
