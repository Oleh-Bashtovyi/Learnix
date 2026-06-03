using System.Text.Json;
using FluentResults;
using Learnix.Application.Common.Caching;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Learnix.Application.Common.Behaviors;

public sealed class CachingBehavior<TRequest, TValue>(
    IDistributedCache cache,
    ILogger<CachingBehavior<TRequest, TValue>> logger)
    : IPipelineBehavior<TRequest, Result<TValue>>
    where TRequest : IRequest<Result<TValue>>, ICacheable<TValue>
{
    public async Task<Result<TValue>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TValue>> next,
        CancellationToken cancellationToken)
    {
        var cached = await cache.GetStringAsync(request.CacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("[CACHE HIT] {Key}", request.CacheKey);
            return Result.Ok(JsonSerializer.Deserialize<TValue>(cached)!);
        }

        var response = await next();

        if (response.IsSuccess)
        {
            var json = JsonSerializer.Serialize(response.Value);
            await cache.SetStringAsync(
                request.CacheKey,
                json,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = request.Expiration },
                cancellationToken);
            logger.LogDebug("[CACHE SET] {Key} (TTL: {Expiration})", request.CacheKey, request.Expiration);
        }

        return response;
    }
}
