using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminDeleteUser;

public sealed record AdminDeleteUserCommand(Guid UserId) : IRequest<Result>;
