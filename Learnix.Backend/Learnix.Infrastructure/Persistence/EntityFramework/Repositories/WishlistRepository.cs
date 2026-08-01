using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class WishlistRepository(ApplicationDbContext context) : IWishlistRepository
{
    public Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
        => context.WishlistItems.AnyAsync(w => w.UserId == userId && w.CourseId == courseId, cancellationToken);

    // The Exists check is a cheap fast path, not the guarantee — (UserId, CourseId) is the composite
    // primary key (WishlistItemConfiguration), so a concurrent add for the same pair is still possible
    // between the check and the insert. This method owns its own save so it can catch exactly that race
    // and treat it as the idempotent success it is, instead of letting a duplicate-key violation reach
    // the caller as an unhandled 500.
    public async Task AddIfMissingAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        if (await ExistsAsync(userId, courseId, cancellationToken))
            return;

        var item = WishlistItem.Create(userId, courseId);
        await context.WishlistItems.AddAsync(item, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Lost the race to a concurrent add for the same pair — already wishlisted, which is the
            // outcome this method promises. Detach so the failed insert doesn't poison a later save on
            // this same context (e.g. the handler's own IUnitOfWork.SaveChangesAsync afterwards).
            context.Entry(item).State = EntityState.Detached;
        }
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public async Task RemoveIfExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
    {
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.CourseId == courseId, cancellationToken);

        if (item is not null)
            context.WishlistItems.Remove(item);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken)
        => context.WishlistItems.CountAsync(w => w.UserId == userId, cancellationToken);

    public Task<List<WishlistItem>> GetPagedAsync(Guid userId, int skip, int take, CancellationToken cancellationToken)
        => context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Include(w => w.Course)
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
}
