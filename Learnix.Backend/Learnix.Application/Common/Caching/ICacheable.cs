namespace Learnix.Application.Common.Caching;

public interface ICacheable<TValue>
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
