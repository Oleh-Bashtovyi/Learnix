using FluentResults;
using Learnix.Application.Common.Commands;
using Learnix.Domain.ValueObjects;
using MediatR;

namespace Learnix.Application.Lessons.Commands.CreateTestLesson;

public sealed record CreateTestLessonCommand(
    Guid CourseId,
    Guid SectionId,
    string Title,
    string? Description,
    int? AttemptLimit,
    int? CooldownMinutes,
    int PassingThreshold,
    IReadOnlyList<QuestionBlueprint> Questions) : IRequest<Result<Guid>>, ICommandWithCourseAndSectionId;
