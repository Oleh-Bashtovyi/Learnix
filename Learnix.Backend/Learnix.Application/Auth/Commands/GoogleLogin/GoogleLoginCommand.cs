using FluentResults;
using Learnix.Application.Auth.Commands.Login;
using MediatR;

namespace Learnix.Application.Auth.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<Result<LoginResponse>>;
