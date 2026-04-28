using FluentResults;
using MediatR;

namespace Learnix.Application.TestAttempts.Queries.GetTestLesson;

public sealed record GetTestLessonQuery(Guid CourseId, Guid LessonId)
    : IRequest<Result<GetTestLessonResponse>>;
