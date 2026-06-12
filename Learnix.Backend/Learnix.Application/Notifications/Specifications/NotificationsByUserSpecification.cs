using Ardalis.Specification;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Application.Notifications.Specifications;

public sealed class NotificationsByUserSpecification : Specification<Notification>
{
    public NotificationsByUserSpecification(Guid userId)
    {
        Query
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(NotificationConstants.MaxPerUser)
            .AsNoTracking();
    }
}
