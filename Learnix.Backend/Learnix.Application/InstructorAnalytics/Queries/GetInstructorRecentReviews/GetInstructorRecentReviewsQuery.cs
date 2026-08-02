using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRecentReviews;

public sealed record GetInstructorRecentReviewsQuery(int Take, Guid? CourseId = null)
    : IRequest<Result<List<InstructorRecentReviewDto>>>;

public sealed record InstructorRecentReviewDto(
    Guid CourseId,
    string CourseTitle,
    string StudentName,
    int Rating,
    string? Text,
    DateTime CreatedAt);
