using FluentResults;
using Learnix.Application.Auth.Commands.Login;
using MediatR;

namespace Learnix.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;
