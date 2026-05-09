using FluentResults;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetMyConversations;

public sealed record GetMyConversationsQuery : IRequest<Result<List<ConversationSummaryDto>>>;
