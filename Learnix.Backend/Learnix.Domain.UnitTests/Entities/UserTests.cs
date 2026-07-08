using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;

namespace Learnix.Domain.UnitTests.Entities;

public class UserTests
{
    [Fact]
    public void Ban_ShouldSetLockoutAndRaiseEvent()
    {
        // Arrange
        var user = new User("test@example.com", "John", "Doe");

        // Act
        user.Ban();

        // Assert
        user.LockoutEnabled.Should().BeTrue();
        user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
        user.DomainEvents.Should().ContainSingle(e => e is UserBannedDomainEvent);
    }

    [Fact]
    public void Unban_ShouldClearLockoutAndRaiseEvent()
    {
        // Arrange
        var user = new User("test@example.com", "John", "Doe");
        user.Ban();
        user.ClearDomainEvents();

        // Act
        user.Unban();

        // Assert
        user.LockoutEnd.Should().BeNull();
        user.DomainEvents.Should().ContainSingle(e => e is UserUnbannedDomainEvent);
    }

    [Fact]
    public void ClaimViaGoogle_ShouldWipePasswordAndConfirmEmail()
    {
        // Arrange
        var user = new User("test@example.com", "John", "Doe");
        user.PasswordHash = "some-old-hash";
        user.EmailConfirmed = false;

        // Act
        user.ClaimViaGoogle("google-123");

        // Assert
        user.GoogleId.Should().Be("google-123");
        user.PasswordHash.Should().BeNull();
        user.EmailConfirmed.Should().BeTrue();
    }
}
