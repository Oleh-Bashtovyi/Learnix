using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.TestAttempts.Commands.StartTestAttempt;
using Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;
using Learnix.Application.TestAttempts.Queries.GetMyTestAttempts;
using Learnix.Application.TestAttempts.Queries.GetTestLesson;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/lessons/{lessonId:guid}/test")]
[Authorize]
public sealed class TestsController(ISender sender) : ControllerBase
{
    /// <summary>Gets test metadata and the current student's status (attempts used, cooldown, etc.).</summary>
    [HttpGet]
    public async Task<IActionResult> GetTestLesson(
        Guid courseId, Guid lessonId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTestLessonQuery(courseId, lessonId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>
    /// Creates an in-progress attempt and returns its ID. Idempotent — returns the existing
    /// in-progress attempt if one already exists for this student and test.
    /// </summary>
    [HttpPost("attempts/start")]
    [EnableRateLimiting(RateLimitPolicies.TestAttempts)]
    public async Task<IActionResult> StartAttempt(
        Guid courseId, Guid lessonId, CancellationToken ct)
    {
        var result = await sender.Send(new StartTestAttemptCommand(courseId, lessonId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Submits answers for an in-progress attempt and scores it.</summary>
    [HttpPost("attempts/{attemptId:guid}/submit")]
    [EnableRateLimiting(RateLimitPolicies.TestAttempts)]
    public async Task<IActionResult> SubmitAttempt(
        Guid courseId, Guid lessonId, Guid attemptId,
        [FromBody] SubmitAttemptRequest request,
        CancellationToken ct)
    {
        var command = new SubmitTestAttemptCommand(courseId, lessonId, attemptId, request.Answers);
        var result = await sender.Send(command, ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Returns all submitted attempts for the current student on this test.</summary>
    [HttpGet("attempts")]
    public async Task<IActionResult> GetMyAttempts(
        Guid courseId, Guid lessonId, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyTestAttemptsQuery(courseId, lessonId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

public sealed record SubmitAttemptRequest(IReadOnlyList<SubmittedAnswerDto> Answers);
