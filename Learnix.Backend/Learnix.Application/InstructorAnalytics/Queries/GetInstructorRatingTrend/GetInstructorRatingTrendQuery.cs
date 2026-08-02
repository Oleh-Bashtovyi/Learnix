using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingTrend;

public sealed record GetInstructorRatingTrendQuery(Guid? CourseId = null)
    : IRequest<Result<List<InstructorRatingTrendItemDto>>>;

/// <param name="Month">Calendar month as yyyy-MM.</param>
public sealed record InstructorRatingTrendItemDto(string Month, double AverageRating, int ReviewCount);
