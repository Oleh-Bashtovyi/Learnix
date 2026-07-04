using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.InstructorApplications.Specifications;

public sealed class ApplicationByIdSpecification : Specification<InstructorApplication>, ISingleResultSpecification<InstructorApplication>
{
    public ApplicationByIdSpecification(Guid id, bool forUpdate = false)
    {
        Query.Where(a => a.Id == id);

        if (!forUpdate)
            Query.AsNoTracking();
    }
}
