using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<Result<RegisterResponse>>;

public sealed record RegisterResponse(Guid UserId, string Email);
