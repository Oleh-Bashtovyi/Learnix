using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Models;
using Learnix.Application.Common.Settings;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Learnix.Infrastructure.Identity;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-001: JWT (short-lived) + Refresh Token
/// - ADR-BACK-AUTH-008: JWT claims — standard OIDC + custom for roles
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction (email_verified claim)
/// </remarks>
internal sealed class JwtTokenService(IOptions<JwtSettings> jwtSettings) : ITokenService
{
    private readonly JwtSettings _settings = jwtSettings.Value;

    public AccessTokenResult GenerateAccessToken(
        Guid userId, string email, string firstName, string lastName, IReadOnlyList<string> roles,
        bool emailConfirmed)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", $"{firstName} {lastName}"),
            new("email_verified", emailConfirmed ? "true" : "false"),
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(serialized, expires);
    }

    public RefreshTokenResult GenerateRefreshToken()
    {
        // 64 random bytes → ~512 bits of entropy
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plain = WebEncoders.Base64UrlEncode(bytes);
        var hash = HashRefreshToken(plain);
        var expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays);
        return new RefreshTokenResult(plain, hash, expires);
    }

    public string HashRefreshToken(string plainToken)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.RefreshTokenSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToBase64String(hashBytes);
    }
}
