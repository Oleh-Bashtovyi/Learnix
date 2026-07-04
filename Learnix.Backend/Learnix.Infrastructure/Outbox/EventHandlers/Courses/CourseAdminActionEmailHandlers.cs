using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.Course;
using Learnix.Infrastructure.Outbox.Payloads;
using Learnix.Infrastructure.Persistence.EntityFramework;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Courses;

internal sealed class CourseAdminDeletedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<CourseAdminDeletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CourseAdminDeletedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var message = await CourseAdminActionEmailHelper.BuildAsync(
            holder.DbContext!, e.EventId, OutboxMessageTypes.CourseAdminDeletedEmail,
            e.InstructorId, e.CourseId, ct);

        if (message is not null)
            holder.DbContext!.OutboxMessages.Add(message);
    }
}

internal sealed class CourseAdminUnpublishedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<CourseAdminUnpublishedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CourseAdminUnpublishedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var message = await CourseAdminActionEmailHelper.BuildAsync(
            holder.DbContext!, e.EventId, OutboxMessageTypes.CourseAdminUnpublishedEmail,
            e.InstructorId, e.CourseId, ct);

        if (message is not null)
            holder.DbContext!.OutboxMessages.Add(message);
    }
}

file static class CourseAdminActionEmailHelper
{
    internal static async Task<OutboxMessage?> BuildAsync(
        ApplicationDbContext db,
        Guid eventId,
        string messageType,
        Guid instructorId,
        Guid courseId,
        CancellationToken ct)
    {
        var instructor = await db.Set<User>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == instructorId)
            .Select(u => new { u.Email, u.FirstName, u.Language })
            .FirstOrDefaultAsync(ct);

        if (instructor is null) return null;

        var courseTitle = await db.Set<Course>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.Title)
            .FirstOrDefaultAsync(ct);

        if (courseTitle is null) return null;

        return OutboxMessage.Create(
            eventId,
            messageType,
            new SendCourseAdminActionEmailPayload(instructor.Email!, instructor.FirstName, courseTitle, instructor.Language));
    }
}
