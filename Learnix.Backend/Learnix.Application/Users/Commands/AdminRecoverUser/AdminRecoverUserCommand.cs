using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminRecoverUser;

public sealed record AdminRecoverUserCommand(Guid UserId) : IRequest<Result>;
