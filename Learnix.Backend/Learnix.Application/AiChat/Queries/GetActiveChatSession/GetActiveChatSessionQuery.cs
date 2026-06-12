using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetActiveChatSession;

public sealed record GetActiveChatSessionQuery : IRequest<Result<ChatSessionDto>>;
