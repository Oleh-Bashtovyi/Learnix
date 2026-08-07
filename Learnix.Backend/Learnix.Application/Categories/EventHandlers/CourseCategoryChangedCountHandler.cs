using Learnix.Application.Categories.Services;
using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using MediatR;

namespace Learnix.Application.Categories.EventHandlers;

internal sealed class CourseCategoryChangedCountHandler(CategoryCoursesCountUpdater updater)
    : INotificationHandler<DomainEventNotification<CourseCategoryChangedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CourseCategoryChangedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        await updater.DecrementAsync(notification.DomainEvent.OldCategoryId, cancellationToken);
        await updater.IncrementAsync(notification.DomainEvent.NewCategoryId, cancellationToken);
    }
}
