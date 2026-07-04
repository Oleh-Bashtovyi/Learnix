using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminAssignRole;

public sealed record AdminAssignRoleCommand(Guid UserId, string Role) : IRequest<Result>;
