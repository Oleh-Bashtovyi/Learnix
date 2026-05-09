using FluentResults;
using MediatR;

namespace Learnix.Application.Messaging.Commands.MarkConversationRead;

public sealed record MarkConversationReadCommand(Guid ConversationId) : IRequest<Result>;
