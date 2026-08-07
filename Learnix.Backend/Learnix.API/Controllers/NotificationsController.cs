using Asp.Versioning;
using Learnix.API.Extensions;
using Learnix.Application.Notifications.Commands.MarkAllNotificationsRead;
using Learnix.Application.Notifications.Commands.MarkNotificationRead;
using Learnix.Application.Notifications.Queries.GetMyNotifications;
using Learnix.Application.Notifications.Queries.GetUnreadNotificationCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyNotificationsQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkNotificationReadCommand(notificationId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("read-by-type")]
    public async Task<IActionResult> MarkReadByType([FromQuery] Learnix.Domain.Enums.NotificationType type, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new Learnix.Application.Notifications.Commands.MarkNotificationsReadByType.MarkNotificationsReadByTypeCommand(type), cancellationToken);
        return result.ToActionResult();
    }
}
