using FluentResults;
using MediatR;

namespace Learnix.Application.Messaging.Commands.UnblockConversation;

public sealed record UnblockConversationCommand(Guid ConversationId) : IRequest<Result>;
