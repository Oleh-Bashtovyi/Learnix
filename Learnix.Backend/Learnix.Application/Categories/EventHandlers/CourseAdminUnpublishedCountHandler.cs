using Learnix.Application.Categories.Services;
using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using MediatR;

namespace Learnix.Application.Categories.EventHandlers;

internal sealed class CourseAdminUnpublishedCountHandler(CategoryCoursesCountUpdater updater)
    : INotificationHandler<DomainEventNotification<CourseAdminUnpublishedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CourseAdminUnpublishedDomainEvent> notification,
        CancellationToken cancellationToken)
        => updater.DecrementAsync(notification.DomainEvent.CategoryId, cancellationToken);
}
