using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Specifications;

public sealed class MessagesCountByConversationSpecification : Specification<CourseMessage>
{
    public MessagesCountByConversationSpecification(Guid conversationId)
    {
        Query.Where(m => m.ConversationId == conversationId);
    }
}
