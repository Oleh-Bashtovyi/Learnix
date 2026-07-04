using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.SetPassword;

public sealed record SetPasswordCommand(string NewPassword) : IRequest<Result>;
