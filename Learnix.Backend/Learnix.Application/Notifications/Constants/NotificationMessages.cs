namespace Learnix.Application.Notifications.Constants;

public static class NotificationMessages
{
    public static string NotificationNotFound => "Notification not found.";
    public static string NotificationIdNotFound(Guid notificationId) => $"Notification {notificationId} not found.";
}
