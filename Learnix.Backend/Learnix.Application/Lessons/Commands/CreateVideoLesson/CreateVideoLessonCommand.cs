using FluentResults;
using Learnix.Application.Common.Commands;
using MediatR;

namespace Learnix.Application.Lessons.Commands.CreateVideoLesson;

public sealed record CreateVideoLessonCommand(
    Guid CourseId,
    Guid SectionId,
    string Title,
    string VideoBlobPath,
    string? Description,
    int? DurationSeconds) : IRequest<Result<Guid>>, ICommandWithCourseAndSectionId;
