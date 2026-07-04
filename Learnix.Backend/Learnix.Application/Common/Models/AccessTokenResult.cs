namespace Learnix.Application.Common.Models;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);

public sealed record RefreshTokenResult(string PlainToken, string TokenHash, DateTime ExpiresAt);
