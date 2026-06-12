using Learnix.API.Extensions;
using Learnix.Application.Messaging.Commands.MarkConversationRead;
using Learnix.Application.Messaging.Commands.SendMessage;
using Learnix.Application.Messaging.Queries.GetConversationMessages;
using Learnix.Application.Messaging.Queries.GetMyConversations;
using Learnix.Application.Messaging.Queries.GetOrStartConversation;
using Learnix.Application.Messaging.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Learnix.API.RateLimiting;
using System.Text.Json.Serialization;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public sealed class MessagesController(ISender sender) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyConversationsQuery(), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetConversationMessagesQuery(conversationId, skip, take), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("conversations/start-or-get")]
    [Authorize(Policy = "EmailConfirmed")]
    [EnableRateLimiting(RateLimitPolicies.ChatMessages)]
    public async Task<IActionResult> StartOrGet(
        [FromBody] StartConversationRequest body,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetOrStartConversationQuery(body.CourseId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    [Authorize(Policy = "EmailConfirmed")]
    [EnableRateLimiting(RateLimitPolicies.ChatMessages)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendChatMessageRequest body,
        CancellationToken ct)
    {
        var result = await sender.Send(new SendMessageCommand(conversationId, body.Content), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPut("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid conversationId, CancellationToken ct)
    {
        var result = await sender.Send(new MarkConversationReadCommand(conversationId), ct);
        return result.ToActionResult();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await sender.Send(new GetUnreadCountQuery(), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

public sealed record StartConversationRequest([property: JsonRequired] Guid CourseId);

public sealed record SendChatMessageRequest([property: JsonRequired] string Content);
