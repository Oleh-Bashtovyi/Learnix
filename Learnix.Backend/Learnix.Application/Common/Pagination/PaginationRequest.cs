using Learnix.Application.Common.Constants;

namespace Learnix.Application.Common.Pagination;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-012: Offset-based pagination via PaginatedResult&lt;T&gt; + PaginationRequest
/// </remarks>
public record PaginationRequest
{
    public int PageIndex { get; init; }
    public int PageSize { get; init; }

    public PaginationRequest(int pageIndex = 0, int pageSize = PaginationConstants.DefaultPageSize)
    {
        PageIndex = Math.Max(0, pageIndex);
        PageSize = Math.Clamp(pageSize, PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize);
    }

    public static PaginationRequest FromOffset(int skip, int take)
    {
        var normalizedTake = Math.Clamp(take, PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize);
        var normalizedSkip = Math.Max(0, skip);
        return new PaginationRequest(normalizedSkip / normalizedTake, normalizedTake);
    }

    public int Skip => PageIndex * PageSize;
    public int Take => PageSize;
}
