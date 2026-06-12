using FluentResults;
using MediatR;

namespace Learnix.Application.Notifications.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery : IRequest<Result<UnreadNotificationCountResponse>>;
