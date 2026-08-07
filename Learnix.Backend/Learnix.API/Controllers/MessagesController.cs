using System.Text.Json.Serialization;
using Asp.Versioning;
using Learnix.API.Constants;
using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
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

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/messages")]
[Authorize]
public sealed class MessagesController(ISender sender) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyConversationsQuery(skip, take, search), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetConversationMessagesQuery(conversationId, skip, take), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("conversations/start-or-get")]
    [Authorize(Policy = AuthPolicies.EmailConfirmed)]
    [EnableRateLimiting(RateLimitPolicies.ChatMessages)]
    public async Task<IActionResult> StartOrGet(
        [FromBody] StartConversationRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrStartConversationQuery(body.CourseId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    [Authorize(Policy = AuthPolicies.EmailConfirmed)]
    [EnableRateLimiting(RateLimitPolicies.ChatMessages)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendChatMessageRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendMessageCommand(conversationId, body.Content), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPut("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkConversationReadCommand(conversationId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUnreadCountQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

public sealed record StartConversationRequest([property: JsonRequired] Guid CourseId);

public sealed record SendChatMessageRequest([property: JsonRequired] string Content);
