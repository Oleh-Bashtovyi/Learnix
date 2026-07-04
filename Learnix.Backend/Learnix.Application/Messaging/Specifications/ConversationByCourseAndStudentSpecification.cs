using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Specifications;

public sealed class ConversationByCourseAndStudentSpecification
    : Specification<CourseConversation>, ISingleResultSpecification<CourseConversation>
{
    public ConversationByCourseAndStudentSpecification(Guid courseId, Guid studentId, bool forUpdate = false)
    {
        Query.Where(c => c.CourseId == courseId && c.StudentId == studentId);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
