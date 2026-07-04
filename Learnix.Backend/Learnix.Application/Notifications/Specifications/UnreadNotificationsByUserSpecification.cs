using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Notifications.Specifications;

public sealed class UnreadNotificationsByUserSpecification : Specification<Notification>
{
    public UnreadNotificationsByUserSpecification(Guid userId)
    {
        Query
            .Where(n => n.UserId == userId && !n.IsRead)
            .AsNoTracking();
    }
}
