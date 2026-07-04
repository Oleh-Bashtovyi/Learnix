using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;
