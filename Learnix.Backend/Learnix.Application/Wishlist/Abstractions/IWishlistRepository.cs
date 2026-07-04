using Learnix.Domain.Entities;

namespace Learnix.Application.Wishlist.Abstractions;

public interface IWishlistRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task AddIfMissingAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task RemoveIfExistsAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<int> CountAsync(Guid userId, CancellationToken ct);
    Task<List<WishlistItem>> GetPagedAsync(Guid userId, int skip, int take, CancellationToken ct);
}
