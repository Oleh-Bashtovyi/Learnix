using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.Course;
using Learnix.Infrastructure.Outbox.Payloads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Courses;

internal sealed class CourseAdminDeletedHandler(OutboxDbContextHolder holder)
    : INotificationHandler<DomainEventNotification<CourseAdminDeletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CourseAdminDeletedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var db = holder.DbContext!;

        var instructor = await db.Set<User>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == e.InstructorId)
            .Select(u => new { u.Email, u.FirstName })
            .FirstOrDefaultAsync(ct);

        if (instructor is null) return;

        var courseTitle = await db.Set<Course>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Id == e.CourseId)
            .Select(c => c.Title)
            .FirstOrDefaultAsync(ct);

        if (courseTitle is null) return;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = e.EventId,
            Type = OutboxMessageTypes.CourseAdminDeletedEmail,
            Payload = JsonSerializer.Serialize(new SendCourseAdminActionEmailPayload(instructor.Email!, instructor.FirstName, courseTitle)),
            OccurredAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
        });
    }
}
