namespace Learnix.Application.Common.Abstractions.Caching;

public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan Expiry { get; }
}
