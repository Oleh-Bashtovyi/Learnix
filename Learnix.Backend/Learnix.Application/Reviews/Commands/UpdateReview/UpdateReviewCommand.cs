using FluentResults;
using MediatR;

namespace Learnix.Application.Reviews.Commands.UpdateReview;

public sealed record UpdateReviewCommand(Guid CourseId, Guid ReviewId, int Rating, string? Comment)
    : IRequest<Result>;
