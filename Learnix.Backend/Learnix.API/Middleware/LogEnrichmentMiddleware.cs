using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Serilog.Context;

namespace Learnix.API.Middleware;

/// <summary>
/// Enriches Serilog's LogContext with a CorrelationId (generated or extracted from headers)
/// and the authenticated user's ID (sub claim).
/// </summary>
public sealed class LogEnrichmentMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Get or generate Correlation ID
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // 2. Add it to the response so the frontend/client knows which ID to report in case of errors
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // 3. Extract UserId from JWT if the user is authenticated
        // Note: JwtTokenService uses standard 'sub' claim for UserId
        var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                     ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Anonymous";

        // 4. Push properties to Serilog's LogContext
        // Every log emitted during this request will automatically contain CorrelationId and UserId fields
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        {
            await next(context);
        }
    }
}
