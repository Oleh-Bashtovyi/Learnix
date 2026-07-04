using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.TestAttempts.Specifications;

public sealed class InProgressAttemptByStudentAndLessonSpecification
    : Specification<TestAttempt>, ISingleResultSpecification<TestAttempt>
{
    public InProgressAttemptByStudentAndLessonSpecification(Guid studentId, Guid lessonId)
    {
        Query
            .Where(a => a.StudentId == studentId && a.TestLessonId == lessonId && a.SubmittedAt == null)
            .AsNoTracking();
    }
}
