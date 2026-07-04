using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetMyConversations;

public sealed record GetMyConversationsQuery(int Skip = 0, int Take = 20, string? SearchQuery = null)
    : IRequest<Result<PaginatedResult<ConversationSummaryDto>>>;
