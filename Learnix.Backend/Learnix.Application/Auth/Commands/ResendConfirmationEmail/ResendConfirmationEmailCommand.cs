using FluentResults;
using MediatR;

namespace Learnix.Application.Auth.Commands.ResendConfirmationEmail;

public sealed record ResendConfirmationEmailCommand(string Email) : IRequest<Result>;
