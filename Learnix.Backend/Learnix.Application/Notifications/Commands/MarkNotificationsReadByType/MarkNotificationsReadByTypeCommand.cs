using FluentResults;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Notifications.Commands.MarkNotificationsReadByType;

public sealed record MarkNotificationsReadByTypeCommand(NotificationType Type) : IRequest<Result>;
