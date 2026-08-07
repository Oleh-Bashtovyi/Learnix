using System.Text.Json.Serialization;
using Asp.Versioning;
using Learnix.API.Constants;
using Learnix.API.Extensions;
using Learnix.Application.Reviews.Commands.CreateReview;
using Learnix.Application.Reviews.Commands.DeleteReview;
using Learnix.Application.Reviews.Commands.UpdateReview;
using Learnix.Application.Reviews.Queries.GetCourseReviews;
using Learnix.Application.Reviews.Queries.GetMyReview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses/{courseId:guid}/reviews")]
[Authorize]
public sealed class CourseReviewsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        Guid courseId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCourseReviewsQuery(courseId, skip, take), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(Guid courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyReviewQuery(courseId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.EmailConfirmed)]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromBody] CreateReviewRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateReviewCommand(courseId, body.Rating, body.Comment), cancellationToken);
        return result.ToActionResult(onSuccess: value => CreatedAtAction(nameof(GetMine), new { courseId }, value));
    }

    [HttpPut("{reviewId:guid}")]
    [Authorize(Policy = AuthPolicies.EmailConfirmed)]
    public async Task<IActionResult> Update(
        Guid courseId,
        Guid reviewId,
        [FromBody] UpdateReviewRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateReviewCommand(courseId, reviewId, body.Rating, body.Comment), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{reviewId:guid}")]
    public async Task<IActionResult> Delete(Guid courseId, Guid reviewId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteReviewCommand(courseId, reviewId), cancellationToken);
        return result.ToActionResult();
    }
}

public sealed record CreateReviewRequest(
    [property: JsonRequired] int Rating,
    string? Comment);

public sealed record UpdateReviewRequest(
    [property: JsonRequired] int Rating,
    string? Comment);
