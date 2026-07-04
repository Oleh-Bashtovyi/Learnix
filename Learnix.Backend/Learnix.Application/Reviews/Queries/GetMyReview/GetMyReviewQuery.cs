using FluentResults;
using MediatR;

namespace Learnix.Application.Reviews.Queries.GetMyReview;

public sealed record GetMyReviewQuery(Guid CourseId) : IRequest<Result<MyReviewDto?>>;
