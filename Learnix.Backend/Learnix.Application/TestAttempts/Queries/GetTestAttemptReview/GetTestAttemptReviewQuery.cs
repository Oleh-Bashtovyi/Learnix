using FluentResults;
using MediatR;

namespace Learnix.Application.TestAttempts.Queries.GetTestAttemptReview;

public sealed record GetTestAttemptReviewQuery(
    Guid CourseId,
    Guid LessonId,
    Guid AttemptId) : IRequest<Result<TestAttemptReviewResponse>>;
