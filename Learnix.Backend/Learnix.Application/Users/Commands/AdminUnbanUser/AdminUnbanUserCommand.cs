using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminUnbanUser;

public sealed record AdminUnbanUserCommand(Guid UserId) : IRequest<Result>;
