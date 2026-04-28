using FluentResults;
using MediatR;

namespace Learnix.Application.TestAttempts.Queries.GetMyTestAttempts;

public sealed record GetMyTestAttemptsQuery(Guid CourseId, Guid LessonId)
    : IRequest<Result<IReadOnlyList<TestAttemptSummaryDto>>>;
