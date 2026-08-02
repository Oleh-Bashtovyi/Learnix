using Learnix.API.Extensions;
using Learnix.Application.InstructorAnalytics.Queries.GetCoursePopularity;
using Learnix.Application.InstructorAnalytics.Queries.GetCourseStatuses;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorEngagement;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorLessonDropOff;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorOverview;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingTrend;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorRecentReviews;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorTestPerformance;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/instructor/analytics")]
[Authorize(Roles = Roles.Instructor)]
public sealed class InstructorAnalyticsController(ISender sender) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorOverviewQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorAnalyticsSummaryQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("dynamics")]
    public async Task<IActionResult> GetDynamics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorAnalyticsDynamicsQuery(startDate, endDate), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("courses/popularity")]
    public async Task<IActionResult> GetCoursePopularity(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCoursePopularityQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("courses/statuses")]
    public async Task<IActionResult> GetCourseStatuses(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseStatusesQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("reviews/distribution")]
    public async Task<IActionResult> GetRatingDistribution([FromQuery] Guid? courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorRatingDistributionQuery(courseId), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("reviews/recent")]
    public async Task<IActionResult> GetRecentReviews(
        [FromQuery] int take = 10,
        [FromQuery] Guid? courseId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInstructorRecentReviewsQuery(take, courseId), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("reviews/trend")]
    public async Task<IActionResult> GetRatingTrend([FromQuery] Guid? courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorRatingTrendQuery(courseId), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("tests/performance")]
    public async Task<IActionResult> GetTestPerformance([FromQuery] Guid? courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorTestPerformanceQuery(courseId), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("engagement")]
    public async Task<IActionResult> GetEngagement(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorEngagementQuery(), cancellationToken);
        return result.ToActionResult(Ok);
    }

    [HttpGet("engagement/drop-off")]
    public async Task<IActionResult> GetLessonDropOff([FromQuery] Guid courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorLessonDropOffQuery(courseId), cancellationToken);
        return result.ToActionResult(Ok);
    }
}
