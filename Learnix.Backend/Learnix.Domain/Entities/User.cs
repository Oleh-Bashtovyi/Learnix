using Learnix.Domain.Common;
using Learnix.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace Learnix.Domain.Entities;

public class User : IdentityUser<Guid>, IAuditable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private User() { }

    public User(string email, string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        Email = email;
        UserName = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public string? GoogleId { get; private set; }

    #pragma warning disable S1144
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    #pragma warning restore S1144

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public void UpdateProfile(string firstName, string lastName, string? bio)
    {
        FirstName = firstName;
        LastName = lastName;
        Bio = bio;
    }

    public void SetAvatar(string avatarUrl) => AvatarUrl = avatarUrl;
    public void SetGoogleId(string googleId) => GoogleId = googleId;

    /// <summary>Raise domain event from outside the entity (e.g. from a command handler after UserManager creates user).</summary>
    /// <remarks>Required because UserManager.CreateAsync persists immediately, before handler can raise events through normal flow.</remarks>
    public void RaiseUserRegistered(string emailConfirmationToken)
        => RaiseDomainEvent(new UserRegisteredDomainEvent(Id, Email!, FirstName, emailConfirmationToken));

    /// <summary>Raise domain event from outside the entity (after Identity generates password reset token).</summary>
    /// <remarks>Same rationale as <see cref="RaiseUserRegistered"/>: called by Application layer after UserManager operation.</remarks>
    public void RaisePasswordResetRequested(string token)
        => RaiseDomainEvent(new PasswordResetRequestedDomainEvent(Id, Email!, FirstName, token));
}