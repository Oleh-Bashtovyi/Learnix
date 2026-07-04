using FluentResults;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetOrStartConversation;

public sealed record GetOrStartConversationQuery(Guid CourseId)
    : IRequest<Result<ConversationDto>>;
