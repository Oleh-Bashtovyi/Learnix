using System.Globalization;
using System.Threading.RateLimiting;
using Learnix.API.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddLearnixRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Unified 429 response for all rate-limited policies
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // Retry-After header — RFC 6585 says seconds (integer)
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                var problem = new ProblemDetails
                {
                    Title = "Too many requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "You've made too many requests. Please try again later."
                };

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(problem, ct);
            };

            // auth-strict: 5 requests per 15 minutes per IP
            options.AddPolicy(RateLimitPolicies.AuthStrict, httpContext =>
            {
                // Partition by IP AND the requested path
                var ip = GetClientIp(httpContext);
                var path = httpContext.Request.Path.ToString().ToLowerInvariant();
                var partitionKey = $"{ip}_{path}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });

            // ai-chat: 20 requests per hour per authenticated user
            options.AddPolicy(RateLimitPolicies.AiChat, httpContext =>
            {
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });

            // test-attempts: 30 requests per minute per authenticated user to prevent bot spamming
            options.AddPolicy(RateLimitPolicies.TestAttempts, httpContext =>
            {
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });

            // payments: 5 requests per minute
            options.AddPolicy(RateLimitPolicies.Payments, httpContext =>
            {
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            // uploads: 20 requests per minute
            options.AddPolicy(RateLimitPolicies.Uploads, httpContext =>
            {
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            // chat-messages: 30 requests per minute
            options.AddPolicy(RateLimitPolicies.ChatMessages, httpContext =>
            {
                var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            // GLOBAL LIMITER: 100 requests per minute for ALL endpoints
            // This runs in addition to any specific policies above.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                   ?? GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Resolves the client IP for partitioning. Prefers X-Forwarded-For when the request
    /// came through a trusted proxy (configured via ForwardedHeadersOptions in Program.cs),
    /// falls back to RemoteIpAddress, and uses a fixed sentinel if both are missing.
    /// </summary>
    private static string GetClientIp(HttpContext httpContext)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        return !string.IsNullOrEmpty(remoteIp) ? remoteIp : "unknown";
    }
}