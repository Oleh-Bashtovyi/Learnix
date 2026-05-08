using FluentResults;
using MediatR;

namespace Learnix.Application.Lessons.Queries.GetLessonContent;

public sealed record GetLessonContentQuery(Guid CourseId, Guid LessonId)
    : IRequest<Result<LessonContentDto>>;
