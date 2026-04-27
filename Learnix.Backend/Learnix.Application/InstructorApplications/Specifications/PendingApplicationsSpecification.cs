using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.InstructorApplications.Specifications;

public sealed class PendingApplicationsSpecification : Specification<InstructorApplication>
{
    public PendingApplicationsSpecification(int skip, int take)
    {
        Query
            .Where(a => a.Status == ApplicationStatus.Pending)
            .Include(a => a.User)
            .OrderBy(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();
    }
}
