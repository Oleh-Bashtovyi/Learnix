using FluentResults;
using MediatR;

namespace Learnix.Application.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(Guid CourseId, int Rating, string? Comment)
    : IRequest<Result<CreateReviewResponse>>;
