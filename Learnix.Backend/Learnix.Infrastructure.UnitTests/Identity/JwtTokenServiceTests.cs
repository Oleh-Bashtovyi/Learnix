using System.IdentityModel.Tokens.Jwt;
using Learnix.Application.Common.Options;
using Learnix.Infrastructure.Constants;
using Learnix.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.UnitTests.Identity;

/// <summary>
/// The token format is owned entirely by this service (ADR-BACK-AUTH-008): the API, the SignalR hub and
/// every authorization policy read the claims it writes here by name, so a wrong claim name or a flipped
/// email_verified value is invisible to anything except a test that reads the token back.
/// </summary>
public class JwtTokenServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly JwtTokenService _sut = new(Options.Create(new JwtOptions
    {
        Issuer = "learnix-tests",
        Audience = "learnix-tests",
        Secret = "0123456789abcdef0123456789abcdef0123456789abcdef",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7,
        RefreshTokenSecret = "fedcba9876543210fedcba9876543210fedcba9876543210"
    }));

    [Fact]
    public void GenerateAccessToken_WritesTheClaimsAuthorizationAndTheApiReadByName()
    {
        var result = _sut.GenerateAccessToken(
            UserId, "ada@learnix.dev", "Ada", "Lovelace", ["Student", "Instructor"], emailConfirmed: true);

        var token = ReadToken(result.Token);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == UserId.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "ada@learnix.dev");
        token.Claims.Should().Contain(c => c.Type == ClaimNames.Name && c.Value == "Ada Lovelace");
        token.Claims.Should().Contain(c => c.Type == ClaimNames.EmailVerified && c.Value == ClaimNames.TrueValue);
        token.Claims.Where(c => c.Type == ClaimNames.Role).Select(c => c.Value)
            .Should().BeEquivalentTo(["Student", "Instructor"]);
    }

    [Fact]
    public void GenerateAccessToken_WhenEmailIsNotConfirmed_WritesEmailVerifiedFalse()
    {
        var result = _sut.GenerateAccessToken(
            UserId, "ada@learnix.dev", "Ada", "Lovelace", [], emailConfirmed: false);

        ReadToken(result.Token).Claims.Should()
            .Contain(c => c.Type == ClaimNames.EmailVerified && c.Value == ClaimNames.FalseValue);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAfterTheConfiguredNumberOfMinutes()
    {
        var before = DateTime.UtcNow;

        var result = _sut.GenerateAccessToken(UserId, "ada@learnix.dev", "Ada", "Lovelace", [], true);

        result.ExpiresAt.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_GivesEveryTokenAUniqueJti()
    {
        // The jti is what lets a caller tell two access tokens for the same user apart (logging, revocation
        // lists). Two calls issued back to back must never collide.
        var first = ReadToken(_sut.GenerateAccessToken(UserId, "a@learnix.dev", "A", "B", [], true).Token);
        var second = ReadToken(_sut.GenerateAccessToken(UserId, "a@learnix.dev", "A", "B", [], true).Token);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesAPlainTokenWhoseHashMatchesHashRefreshToken()
    {
        // The plain token is what goes in the cookie; only its hash is stored (ADR-BACK-AUTH-001). If the
        // two ever diverge, every legitimate refresh starts failing.
        var refreshToken = _sut.GenerateRefreshToken();

        _sut.HashRefreshToken(refreshToken.PlainToken).Should().Be(refreshToken.TokenHash);
    }

    [Fact]
    public void GenerateRefreshToken_ExpiresAfterTheConfiguredNumberOfDays()
    {
        var before = DateTime.UtcNow;

        var result = _sut.GenerateRefreshToken();

        result.ExpiresAt.Should().BeCloseTo(before.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic_ForTheSamePlainToken()
        // The DB lookup on refresh hashes the incoming cookie and matches it by equality — a
        // non-deterministic hash would make every refresh token unusable after the first request.
        => _sut.HashRefreshToken("some-plain-token").Should().Be(_sut.HashRefreshToken("some-plain-token"));

    [Fact]
    public void HashRefreshToken_DiffersForDifferentTokens()
        => _sut.HashRefreshToken("token-a").Should().NotBe(_sut.HashRefreshToken("token-b"));

    private static JwtSecurityToken ReadToken(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);
}
