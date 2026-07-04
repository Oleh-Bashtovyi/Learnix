using Learnix.Application.Categories.Services;
using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using MediatR;

namespace Learnix.Application.Categories.EventHandlers;

internal sealed class CourseUnpublishedCountHandler(CategoryCoursesCountUpdater updater)
    : INotificationHandler<DomainEventNotification<CourseUnpublishedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CourseUnpublishedDomainEvent> notification,
        CancellationToken cancellationToken)
        => updater.DecrementAsync(notification.DomainEvent.CategoryId, cancellationToken);
}
