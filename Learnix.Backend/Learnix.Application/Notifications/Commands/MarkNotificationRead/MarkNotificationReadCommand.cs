using FluentResults;
using MediatR;

namespace Learnix.Application.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;
