using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Courses;

internal sealed class CourseAdminUnpublishedCountHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<CourseAdminUnpublishedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CourseAdminUnpublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var ctx = holder.DbContext;
        if (ctx is null) return;

        var category = await ctx.Categories
            .FirstOrDefaultAsync(c => c.Id == notification.DomainEvent.CategoryId, cancellationToken);

        category?.DecrementCoursesCount();
    }
}
