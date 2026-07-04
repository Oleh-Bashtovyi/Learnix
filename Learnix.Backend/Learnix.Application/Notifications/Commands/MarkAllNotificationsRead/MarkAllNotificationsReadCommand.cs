using FluentResults;
using MediatR;

namespace Learnix.Application.Notifications.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand : IRequest<Result>;
