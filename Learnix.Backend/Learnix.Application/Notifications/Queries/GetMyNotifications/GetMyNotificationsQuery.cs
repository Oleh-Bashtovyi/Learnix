using FluentResults;
using MediatR;

namespace Learnix.Application.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery : IRequest<Result<IReadOnlyList<NotificationDto>>>;
