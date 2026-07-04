using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.TestAttempts.Specifications;

public sealed class TestAttemptsByStudentAndLessonSpecification : Specification<TestAttempt>
{
    public TestAttemptsByStudentAndLessonSpecification(Guid studentId, Guid lessonId)
    {
        Query
            .Where(a => a.StudentId == studentId && a.TestLessonId == lessonId && a.SubmittedAt != null)
            .OrderByDescending(a => a.AttemptNumber)
            .AsNoTracking();
    }
}
