using FluentResults;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<Result<UnreadCountDto>>;
