using FluentResults;
using MediatR;

namespace Learnix.Application.LessonProgress.Commands.MarkLessonComplete;

public sealed record MarkLessonCompleteCommand(Guid CourseId, Guid LessonId)
    : IRequest<Result<MarkLessonCompleteResponse>>;
