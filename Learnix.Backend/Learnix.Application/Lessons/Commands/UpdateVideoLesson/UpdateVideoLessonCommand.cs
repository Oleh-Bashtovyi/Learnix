using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Lessons.Commands.UpdateVideoLesson;

public sealed record UpdateVideoLessonCommand(
    Guid CourseId,
    Guid LessonId,
    string Title,
    string VideoBlobPath,
    string? Description,
    int? DurationSeconds) : IRequest<Result>, ICommandWithCourseId;
