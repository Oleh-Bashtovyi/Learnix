using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
