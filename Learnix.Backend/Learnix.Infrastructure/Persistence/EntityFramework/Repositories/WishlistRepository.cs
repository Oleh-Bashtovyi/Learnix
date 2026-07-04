using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class WishlistRepository(ApplicationDbContext context) : IWishlistRepository
{
    public Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken ct)
        => context.WishlistItems.AnyAsync(w => w.UserId == userId && w.CourseId == courseId, ct);

    public async Task AddIfMissingAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var exists = await ExistsAsync(userId, courseId, ct);
        if (!exists)
            await context.WishlistItems.AddAsync(WishlistItem.Create(userId, courseId), ct);
    }

    public async Task RemoveIfExistsAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.CourseId == courseId, ct);

        if (item is not null)
            context.WishlistItems.Remove(item);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken ct)
        => context.WishlistItems.CountAsync(w => w.UserId == userId, ct);

    public Task<List<WishlistItem>> GetPagedAsync(Guid userId, int skip, int take, CancellationToken ct)
        => context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Include(w => w.Course)
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
}
