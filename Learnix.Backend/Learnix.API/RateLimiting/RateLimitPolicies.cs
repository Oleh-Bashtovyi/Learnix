namespace Learnix.API.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>
    /// Strict per-IP limit for sensitive auth endpoints:
    /// register, login, google login, forgot-password, reset-password,
    /// resend-confirmation, confirm-email.
    /// 5 requests per 15 minutes per IP.
    /// </summary>
    public const string AuthStrict = "auth-strict";

    /// <summary>
    /// Per-user limit for AI chat messages endpoint.
    /// 20 requests per hour per authenticated user.
    /// </summary>
    public const string AiChat = "ai-chat";
}