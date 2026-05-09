using FluentResults;
using MediatR;

namespace Learnix.Application.Messaging.Commands.SendMessage;

public sealed record SendMessageCommand(Guid ConversationId, string Content)
    : IRequest<Result<SendMessageResponse>>;
