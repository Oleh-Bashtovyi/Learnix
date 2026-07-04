using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetConversationMessages;

public sealed record GetConversationMessagesQuery(
    Guid ConversationId,
    int Skip,
    int Take) : IRequest<Result<PaginatedResult<MessageDto>>>;
