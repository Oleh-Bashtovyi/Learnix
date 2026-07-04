using FluentResults;
using MediatR;

namespace Learnix.Application.TestAttempts.Commands.StartTestAttempt;

public sealed record StartTestAttemptCommand(
    Guid CourseId,
    Guid LessonId) : IRequest<Result<StartTestAttemptResponse>>;
