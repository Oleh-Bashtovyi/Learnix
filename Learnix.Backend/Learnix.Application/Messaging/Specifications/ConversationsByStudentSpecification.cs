using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Specifications;

public sealed class ConversationsByStudentSpecification : Specification<CourseConversation>
{
    public ConversationsByStudentSpecification(Guid studentId)
    {
        Query
            .Where(c => c.StudentId == studentId)
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .OrderByDescending(c => c.LastMessageAt)
            .AsNoTracking();
    }
}
