using Learnix.Application.Categories.Services;
using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using MediatR;

namespace Learnix.Application.Categories.EventHandlers;

internal sealed class CourseArchivedCountHandler(CategoryCoursesCountUpdater updater)
    : INotificationHandler<DomainEventNotification<CourseArchivedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CourseArchivedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        if (!notification.DomainEvent.WasPublished) return Task.CompletedTask;
        return updater.DecrementAsync(notification.DomainEvent.CategoryId, cancellationToken);
    }
}
