using Learnix.Domain.Common;
using Learnix.Domain.Events;
using Learnix.Domain.Events.User;
using Microsoft.AspNetCore.Identity;

namespace Learnix.Domain.Entities;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-002: ASP.NET Identity — inherit from IdentityUser, custom DbContext
/// - ADR-BACK-AUTH-003: Pure Identity roles instead of UserRole enum
/// - ADR-BACK-AUTH-011: GoogleId as denormalized field on User
/// </remarks>
public class User : IdentityUser<Guid>, IAuditable, IHasDomainEvents, ISoftDeletable
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
    public string Language { get; private set; } = "en";
    public string? AvatarBlobPath { get; private set; }
    public string? Bio { get; private set; }
    public string? GoogleId { get; private set; }
    public bool IsDeleted { get; private set; } = false;
    public DateTime? DeletedAt { get; private set; } = null;

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
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id));
    }

    public void SetAvatar(string newBlobPath)
    {
        if (AvatarBlobPath is not null)
            RaiseDomainEvent(new UserAvatarRemovedDomainEvent(Id, AvatarBlobPath));

        AvatarBlobPath = newBlobPath;
        RaiseDomainEvent(new UserAvatarSetDomainEvent(Id, newBlobPath));
    }
    public void SetLanguage(string language) => Language = language;
    public void SetGoogleId(string googleId) => GoogleId = googleId;

    /// <summary>
    /// Takeover scenario: existing account with unconfirmed email is being claimed via Google OAuth.
    /// Google has now verified ownership of this email, so we wipe any pre-existing password
    /// (possibly set by an attacker who registered the email but never confirmed it), 
    /// mark the email as confirmed, and link the Google account.
    /// </summary>
    public void ClaimViaGoogle(string googleId)
    {
        GoogleId = googleId;
        PasswordHash = null;
        EmailConfirmed = true;
    }

    /// <summary>
    /// Confirm email based on a verified external provider (e.g., Google email_verified claim).
    /// Skips the standard email token flow because ownership is already proven.
    /// </summary>
    public void ConfirmEmailFromGoogle() => EmailConfirmed = true;

    public void Ban()
    {
        LockoutEnabled = true;
        LockoutEnd = DateTimeOffset.MaxValue;
        RaiseDomainEvent(new UserBannedDomainEvent(Id));
    }

    public void Unban()
    {
        LockoutEnd = null;
        RaiseDomainEvent(new UserUnbannedDomainEvent(Id));
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Recover()
    {
        IsDeleted = false;
        DeletedAt = null;
    }

    /// <summary>Raise domain event from outside the entity (e.g. from a command handler after UserManager creates user).</summary>
    /// <remarks>Required because UserManager.CreateAsync persists immediately, before handler can raise events through normal flow.</remarks>
    public void RaiseUserRegistered(string emailConfirmationToken)
        => RaiseDomainEvent(new UserRegisteredDomainEvent(Id, Email!, FirstName, emailConfirmationToken));

    /// <summary>Raise domain event from outside the entity (after Identity generates password reset token).</summary>
    /// <remarks>Same rationale as <see cref="RaiseUserRegistered"/>: called by Application layer after UserManager operation.</remarks>
    public void RaisePasswordResetRequested(string token)
        => RaiseDomainEvent(new PasswordResetRequestedDomainEvent(Id, Email!, FirstName, token));

    /// <summary>Raise domain event from outside the entity (after UserManager role operation completes).</summary>
    /// <remarks>Same rationale as <see cref="RaiseUserRegistered"/>: Identity operations persist immediately.</remarks>
    public void RaiseRoleChanged(string role, bool assigned)
        => RaiseDomainEvent(new UserRoleChangedDomainEvent(Id, role, assigned));
}
