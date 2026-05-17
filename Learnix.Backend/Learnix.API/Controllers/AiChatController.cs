using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.AiChat.Commands.ClearChatSession;
using Learnix.Application.AiChat.Queries.GetActiveChatSession;
using Learnix.Application.AiChat.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Text.Json;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/ai-chat")]
[Authorize]
public sealed class AiChatController(ISender sender, ChatStreamOrchestrator orchestrator) : ControllerBase
{
    [HttpGet("session")]
    public async Task<IActionResult> GetSession(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(new GetActiveChatSessionQuery(userId.Value), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("messages")]
    [EnableRateLimiting(RateLimitPolicies.AiChat)]
    public async Task StreamMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Message cannot be empty" }, ct);
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var sseEvent in orchestrator.StreamAsync(userId.Value, request.Message, ct))
        {
            var line = $"event: {sseEvent.EventType}\ndata: {sseEvent.Data}\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(line), ct);
            await Response.Body.FlushAsync(ct);

            if (sseEvent.EventType == "message_end")
                break;
        }
    }

    [HttpDelete("session")]
    public async Task<IActionResult> ClearSession(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await sender.Send(new ClearChatSessionCommand(userId.Value), ct);
        return result.ToActionResult();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record SendMessageRequest(string Message);
