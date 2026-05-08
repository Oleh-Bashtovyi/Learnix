using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Course;
using Learnix.Infrastructure.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Courses.EventHandlers;

internal sealed class CoursePublishedCountHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<CoursePublishedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CoursePublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var ctx = holder.DbContext;
        if (ctx is null) return;

        var category = await ctx.Categories
            .FirstOrDefaultAsync(c => c.Id == notification.DomainEvent.CategoryId, cancellationToken);

        category?.IncrementCoursesCount();
    }
}
