using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using Learnix.Infrastructure.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Courses.EventHandlers;

internal sealed class CourseArchivedCountHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<CourseArchivedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CourseArchivedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        if (!notification.DomainEvent.WasPublished) return;

        var ctx = holder.DbContext;
        if (ctx is null) return;

        var category = await ctx.Categories
            .FirstOrDefaultAsync(c => c.Id == notification.DomainEvent.CategoryId, cancellationToken);

        category?.DecrementCoursesCount();
    }
}
