using FluentResults;
using Learnix.Application.Common.Commands;
using Learnix.Domain.ValueObjects;
using MediatR;

namespace Learnix.Application.Lessons.Commands.UpdateTestLesson;

public sealed record UpdateTestLessonCommand(
    Guid CourseId,
    Guid LessonId,
    string Title,
    string? Description,
    int? AttemptLimit,
    int? CooldownMinutes,
    int PassingThreshold,
    IReadOnlyList<QuestionBlueprint> Questions) : IRequest<Result>, ICommandWithCourseId;
