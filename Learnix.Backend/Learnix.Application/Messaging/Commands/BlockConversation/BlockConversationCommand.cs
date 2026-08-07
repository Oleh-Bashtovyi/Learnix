using FluentResults;
using MediatR;

namespace Learnix.Application.Messaging.Commands.BlockConversation;

public sealed record BlockConversationCommand(Guid ConversationId) : IRequest<Result>;
