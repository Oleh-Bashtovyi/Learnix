using FluentResults;
using Learnix.Application.Auth.Commands.Login;
using MediatR;

namespace Learnix.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Language = "en"
) : IRequest<Result<LoginResponse>>;

public sealed record RegisterResponse(Guid UserId, string Email);
