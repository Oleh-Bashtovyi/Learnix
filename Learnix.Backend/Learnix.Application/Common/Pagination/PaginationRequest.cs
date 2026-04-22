namespace Learnix.Application.Common.Pagination;

public record PaginationRequest
{
    public const int MaxPageSize = 100;
    public const int MinPageSize = 1;
    public const int DefaultPageSize = 20;

    public int PageIndex { get; init; }
    public int PageSize { get; init; }

    public PaginationRequest(int pageIndex = 0, int pageSize = DefaultPageSize)
    {
        PageIndex = Math.Max(0, pageIndex);
        PageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);
    }

    public static PaginationRequest FromOffset(int skip, int take)
    {
        var normalizedTake = Math.Clamp(take, MinPageSize, MaxPageSize);
        var normalizedSkip = Math.Max(0, skip);
        return new PaginationRequest(normalizedSkip / normalizedTake, normalizedTake);
    }

    public int Skip => PageIndex * PageSize;
    public int Take => PageSize;
}
