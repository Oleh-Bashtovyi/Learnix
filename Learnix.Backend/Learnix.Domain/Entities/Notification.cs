using Learnix.Domain.Common;
using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public sealed class Notification : BaseEntity
{
    private Notification() { }

    private Notification(Guid userId, NotificationType type, string title, string body)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public bool IsRead { get; private set; }

    public static Notification Create(Guid userId, NotificationType type, string title, string body)
        => new(userId, type, title, body);

    public void MarkRead() => IsRead = true;
}
