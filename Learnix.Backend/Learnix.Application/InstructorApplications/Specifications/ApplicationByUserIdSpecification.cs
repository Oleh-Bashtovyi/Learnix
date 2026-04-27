using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorApplications.Specifications;

public sealed class ApplicationByUserIdSpecification : Specification<InstructorApplication>, ISingleResultSpecification<InstructorApplication>
{
    public ApplicationByUserIdSpecification(Guid userId, bool forUpdate = false)
    {
        Query.Where(a => a.UserId == userId);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
