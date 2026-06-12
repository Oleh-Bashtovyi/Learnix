using Learnix.Domain.Enums;

namespace Learnix.Application.Notifications.Abstractions;

public interface INotificationSender
{
    Task SendAsync(Guid userId, NotificationType type, string title, string body, CancellationToken ct = default);
}
