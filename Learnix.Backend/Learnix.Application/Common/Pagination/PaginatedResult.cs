namespace Learnix.Application.Common.Pagination;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-012: Offset-based pagination via PaginatedResult&lt;T&gt; + PaginationRequest
/// </remarks>
public record PaginatedResult<TEntity>(
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyList<TEntity> Items
) where TEntity : class
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages - 1;
    public bool HasPreviousPage => Page > 0;

    public static PaginatedResult<TEntity> Create(
        IEnumerable<TEntity> items, int pageIndex, int pageSize, long totalCount)
        => new(pageIndex, pageSize, totalCount, items.ToList().AsReadOnly());

    public static PaginatedResult<TEntity> Empty(int pageIndex, int pageSize)
        => new(pageIndex, pageSize, 0, Array.Empty<TEntity>());
}
