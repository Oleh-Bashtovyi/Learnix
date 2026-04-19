using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
