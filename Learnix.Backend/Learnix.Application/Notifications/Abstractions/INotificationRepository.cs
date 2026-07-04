using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Notifications.Abstractions;

public interface INotificationRepository : IRepositoryBase<Notification>
{
    Task TrimToMaxAsync(Guid userId, int maxCount, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllReadByTypeAsync(Guid userId, Learnix.Domain.Enums.NotificationType type, CancellationToken ct = default);
}
