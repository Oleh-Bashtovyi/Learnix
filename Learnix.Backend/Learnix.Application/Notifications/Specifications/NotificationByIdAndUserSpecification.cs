using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Notifications.Specifications;

public sealed class NotificationByIdAndUserSpecification : Specification<Notification>, ISingleResultSpecification<Notification>
{
    public NotificationByIdAndUserSpecification(Guid notificationId, Guid userId)
    {
        Query.Where(n => n.Id == notificationId && n.UserId == userId);
    }
}
