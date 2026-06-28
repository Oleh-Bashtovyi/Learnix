namespace Learnix.Application.Achievements.Abstractions;

public interface IUserCompletedCategoryRepository
{
    /// <summary>
    /// Idempotent insert. Returns true if a new row was added,
    /// false if the (UserId, CategoryId) pair already existed.
    /// </summary>
    Task<bool> AddIfMissingAsync(Guid userId, Guid categoryId, CancellationToken ct);

    Task<int> CountDistinctCategoriesAsync(Guid userId, CancellationToken ct);
}
