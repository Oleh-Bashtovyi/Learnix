using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Specifications;

public sealed class ConversationsByInstructorSpecification : Specification<CourseConversation>
{
    public ConversationsByInstructorSpecification(Guid instructorId)
    {
        Query
            .Where(c => c.InstructorId == instructorId)
            .Include(c => c.Course)
            .Include(c => c.Student)
            .OrderByDescending(c => c.LastMessageAt)
            .AsNoTracking();
    }
}
