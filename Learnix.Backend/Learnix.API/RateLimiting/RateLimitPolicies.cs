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

    /// <summary>
    /// Per-user limit for test attempt operations (start, submit) to prevent bot spamming.
    /// 30 requests per minute per authenticated user.
    /// </summary>
    public const string TestAttempts = "test-attempts";

    /// <summary>
    /// Limit for creating payment checkouts to avoid Stripe API abuse.
    /// 5 requests per minute per user.
    /// </summary>
    public const string Payments = "payments";

    /// <summary>
    /// Limit for requesting upload signed URLs to prevent storage DoS.
    /// 20 requests per minute per user.
    /// </summary>
    public const string Uploads = "uploads";

    /// <summary>
    /// Limit for sending chat messages to prevent chat spamming.
    /// 30 requests per minute per user.
    /// </summary>
    public const string ChatMessages = "chat-messages";
}
