using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminRemoveRole;

public sealed record AdminRemoveRoleCommand(Guid UserId, string Role) : IRequest<Result>;
