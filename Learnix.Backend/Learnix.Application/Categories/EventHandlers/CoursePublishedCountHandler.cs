using Learnix.Application.Categories.Services;
using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using MediatR;

namespace Learnix.Application.Categories.EventHandlers;

internal sealed class CoursePublishedCountHandler(CategoryCoursesCountUpdater updater)
    : INotificationHandler<DomainEventNotification<CoursePublishedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CoursePublishedDomainEvent> notification,
        CancellationToken cancellationToken)
        => updater.IncrementAsync(notification.DomainEvent.CategoryId, cancellationToken);
}
