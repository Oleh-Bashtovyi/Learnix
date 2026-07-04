using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Commands.ClearChatSession;

public sealed record ClearChatSessionCommand : IRequest<Result>;
