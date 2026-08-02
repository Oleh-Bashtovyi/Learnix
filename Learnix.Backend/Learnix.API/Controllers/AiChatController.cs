using System.Text;
using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Commands.ClearChatSession;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetAiChatStatus;
using Learnix.Application.AiChat.Queries.GetChatSession;
using Learnix.Application.AiChat.Services;
using Learnix.Application.Common.Abstractions.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Learnix.API.Controllers;

/// <summary>
/// A chat session is identified by the signed-in user and the scope in the route (ADR-BACK-CHAT-004).
/// The platform assistant and a course tutor are separate conversations.
/// </summary>
[ApiController]
[Route("api/ai-chat")]
[Authorize]
public sealed class AiChatController(
    ISender sender,
    ChatStreamOrchestrator orchestrator,
    ChatScopeAuthorizer authorizer,
    ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Whether the assistant can answer right now. Read from what the last real chat turns learned about the
    /// provider — the endpoint never calls it, because on a free tier a health-check ping spends the very
    /// quota it is checking (ADR-BACK-CHAT-014).
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAiChatStatusQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("platform/session")]
    public Task<IActionResult> GetPlatformSession(CancellationToken cancellationToken) =>
        GetSession(ChatScope.Platform, cancellationToken);

    [HttpGet("courses/{courseId:guid}/session")]
    public Task<IActionResult> GetCourseSession(Guid courseId, CancellationToken cancellationToken) =>
        GetSession(ChatScope.ForCourse(courseId), cancellationToken);

    [HttpDelete("platform/session")]
    public Task<IActionResult> ClearPlatformSession(CancellationToken cancellationToken) =>
        ClearSession(ChatScope.Platform, cancellationToken);

    [HttpDelete("courses/{courseId:guid}/session")]
    public Task<IActionResult> ClearCourseSession(Guid courseId, CancellationToken cancellationToken) =>
        ClearSession(ChatScope.ForCourse(courseId), cancellationToken);

    [HttpPost("platform/messages")]
    [EnableRateLimiting(RateLimitPolicies.AiChatPlatform)]
    public Task StreamPlatformMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken) =>
        StreamMessage(ChatScope.Platform, request.Message, lessonId: null, cancellationToken);

    [HttpPost("courses/{courseId:guid}/messages")]
    [EnableRateLimiting(RateLimitPolicies.AiChatTutor)]
    public Task StreamCourseMessage(
        Guid courseId,
        [FromBody] SendCourseMessageRequest request,
        CancellationToken cancellationToken) =>
        StreamMessage(ChatScope.ForCourse(courseId), request.Message, request.LessonId, cancellationToken);

    private async Task<IActionResult> GetSession(ChatScope scope, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetChatSessionQuery(scope), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    private async Task<IActionResult> ClearSession(ChatScope scope, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ClearChatSessionCommand(scope), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// The stream bypasses the MediatR pipeline, so validation and the scope check run here, before any
    /// SSE header is written — a rejected request must still be able to answer with a status code.
    /// </summary>
    private async Task StreamMessage(ChatScope scope, string message, Guid? lessonId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userId = currentUser.UserId.Value;

        if (string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Message cannot be empty" }, cancellationToken);
            return;
        }

        if (message.Length > AiChatConstants.MessageMaxLength)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = $"Message cannot exceed {AiChatConstants.MessageMaxLength} characters." },
                cancellationToken);
            return;
        }

        var access = await authorizer.EnsureAccessAsync(userId, scope, cancellationToken);
        if (access.IsFailed)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            await Response.WriteAsJsonAsync(new { error = access.Errors[0].Message }, cancellationToken);
            return;
        }

        // A provider we already know is out of quota gets no request: answering 503 here, before the SSE
        // headers, is the difference between a message the client can show and a stream that just dies.
        var status = await sender.Send(new GetAiChatStatusQuery(), cancellationToken);
        if (status.IsSuccess && !status.Value.Available)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(
                new { code = status.Value.Reason, retryAtUtc = status.Value.RetryAtUtc },
                cancellationToken);
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var sseEvent in orchestrator.StreamAsync(userId, scope, lessonId, message, cancellationToken))
        {
            var line = $"event: {sseEvent.EventType}\ndata: {sseEvent.Data}\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(line), cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            if (sseEvent.EventType == ChatSseEventTypes.MessageEnd)
                break;
        }
    }
}

public sealed record SendMessageRequest(string Message);

/// <param name="LessonId">The lesson the student has open, if any. Not a claim about access — it is re-checked.</param>
public sealed record SendCourseMessageRequest(string Message, Guid? LessonId);
