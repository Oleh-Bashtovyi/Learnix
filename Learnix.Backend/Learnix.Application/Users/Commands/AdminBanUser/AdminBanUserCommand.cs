using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Commands.AdminBanUser;

public sealed record AdminBanUserCommand(Guid UserId) : IRequest<Result>;
