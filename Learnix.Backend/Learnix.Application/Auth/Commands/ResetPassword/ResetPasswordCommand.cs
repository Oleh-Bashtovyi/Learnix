using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest<Result>;
