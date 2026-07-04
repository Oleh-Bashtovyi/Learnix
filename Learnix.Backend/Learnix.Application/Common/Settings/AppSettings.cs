namespace Learnix.Application.Common.Settings;

public class AppSettings
{
    // Required for links generation in emails: {ClientBaseUrl}/verify-email?userId=...&token=...
    public string ClientBaseUrl { get; init; } = null!;  // e.g. "http://localhost:5173"
}
