using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class NotificationRepository(ApplicationDbContext context)
    : RepositoryBase<Notification>(context), INotificationRepository
{
    public async Task TrimToMaxAsync(Guid userId, int maxCount, CancellationToken ct = default)
    {
        var excess = await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(maxCount)
            .Select(n => n.Id)
            .ToListAsync(ct);

        if (excess.Count > 0)
            await context.Notifications
                .Where(n => excess.Contains(n.Id))
                .ExecuteDeleteAsync(ct);
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
        => context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

    public Task MarkAllReadByTypeAsync(Guid userId, Learnix.Domain.Enums.NotificationType type, CancellationToken ct = default)
        => context.Notifications
            .Where(n => n.UserId == userId && n.Type == type && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
}
