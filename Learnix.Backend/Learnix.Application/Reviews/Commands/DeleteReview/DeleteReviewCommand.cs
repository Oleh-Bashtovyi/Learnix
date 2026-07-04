using FluentResults;
using MediatR;

namespace Learnix.Application.Reviews.Commands.DeleteReview;

public sealed record DeleteReviewCommand(Guid CourseId, Guid ReviewId) : IRequest<Result>;
