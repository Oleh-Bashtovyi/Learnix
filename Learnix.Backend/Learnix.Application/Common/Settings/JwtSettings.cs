namespace Learnix.Application.Common.Settings;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string Secret { get; init; } = null!;
    public int AccessTokenExpiryMinutes { get; init; }
    public int RefreshTokenExpiryDays { get; init; }
    public string RefreshTokenSecret { get; init; } = null!;
}
