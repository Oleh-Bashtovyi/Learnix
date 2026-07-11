using Learnix.Domain.Entities;

namespace Learnix.Domain.UnitTests.Entities;

public class RefreshTokenTests
{
    private static RefreshToken Live()
        => new(Guid.NewGuid(), "hashed-token", DateTime.UtcNow.AddDays(7));

    private static RefreshToken Expired()
        => new(Guid.NewGuid(), "hashed-token", DateTime.UtcNow.AddSeconds(-1));

    [Fact]
    public void IsActive_WhenNeitherRevokedNorExpired_ShouldBeTrue()
    {
        // Act
        var token = Live();

        // Assert
        token.IsActive.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldBeFalse()
    {
        // Act
        var token = Expired();

        // Assert — expiry alone kills the token; no revocation needed
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_ShouldDeactivateAnOtherwiseValidToken()
    {
        // Arrange — rotation revokes the old token on every refresh; a revoked token must never
        // be usable again, even though its expiry is still in the future.
        var token = Live();

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        token.IsActive.Should().BeFalse();
    }
}
