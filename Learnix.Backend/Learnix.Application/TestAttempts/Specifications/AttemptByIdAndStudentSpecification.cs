using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.TestAttempts.Specifications;

/// <summary>
/// Returns an attempt (submitted or in-progress) that belongs to the given student.
/// Used by SubmitTestAttemptCommandHandler to verify ownership before submitting.
/// </summary>
public sealed class AttemptByIdAndStudentSpecification
    : Specification<TestAttempt>, ISingleResultSpecification<TestAttempt>
{
    public AttemptByIdAndStudentSpecification(Guid attemptId, Guid studentId)
    {
        Query.Where(a => a.Id == attemptId && a.StudentId == studentId);
        // No AsNoTracking — used in write path (Submit mutates the attempt)
    }
}
