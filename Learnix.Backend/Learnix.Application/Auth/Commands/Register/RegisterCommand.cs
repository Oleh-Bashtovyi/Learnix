using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Language = "en"
) : IRequest<Result<RegisterResponse>>;

public sealed record RegisterResponse(Guid UserId, string Email);
