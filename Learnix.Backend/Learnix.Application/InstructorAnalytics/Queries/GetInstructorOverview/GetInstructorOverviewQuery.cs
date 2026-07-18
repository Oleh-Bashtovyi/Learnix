using FluentResults;
using Learnix.Application.InstructorAnalytics.Queries.GetCoursePopularity;
using Learnix.Application.InstructorAnalytics.Queries.GetCourseStatuses;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorOverview;

public sealed record GetInstructorOverviewQuery : IRequest<Result<InstructorOverviewDto>>;

/// <summary>
/// Everything the dashboard's first paint needs, in one round trip: the headline summary and the three
/// parameter-less charts. Date-driven (<c>dynamics</c>) and paginated (<c>recent reviews</c>,
/// <c>test performance</c>) data stay on their own endpoints, loaded lazily.
/// </summary>
public sealed record InstructorOverviewDto(
    InstructorAnalyticsSummaryDto Summary,
    CourseStatusesDto CourseStatuses,
    List<CoursePopularityItemDto> Popularity,
    InstructorRatingDistributionDto RatingDistribution);
