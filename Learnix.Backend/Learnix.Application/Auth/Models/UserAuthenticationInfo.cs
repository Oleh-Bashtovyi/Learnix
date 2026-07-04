namespace Learnix.Application.Auth.Models;

public sealed record UserAuthenticationInfo(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool EmailConfirmed,
    string? AvatarBlobPath = null);
