using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Specifications;

public sealed class ConversationByIdSpecification
    : Specification<CourseConversation>, ISingleResultSpecification<CourseConversation>
{
    public ConversationByIdSpecification(Guid id, bool forUpdate = false)
    {
        Query.Where(c => c.Id == id);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
