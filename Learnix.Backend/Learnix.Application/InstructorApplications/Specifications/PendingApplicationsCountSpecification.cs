using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.InstructorApplications.Specifications;

public sealed class PendingApplicationsCountSpecification : Specification<InstructorApplication>
{
    public PendingApplicationsCountSpecification()
    {
        Query
            .AsNoTracking()
            .Where(a => a.Status == ApplicationStatus.Pending);
    }
}
