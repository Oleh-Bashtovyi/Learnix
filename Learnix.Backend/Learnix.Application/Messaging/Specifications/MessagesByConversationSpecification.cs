using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Specifications;

public sealed class MessagesByConversationSpecification : Specification<CourseMessage>
{
    public MessagesByConversationSpecification(Guid conversationId, int skip, int take)
    {
        Query
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();
    }
}
