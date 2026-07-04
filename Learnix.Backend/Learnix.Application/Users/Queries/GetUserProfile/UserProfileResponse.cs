namespace Learnix.Application.Users.Queries.GetUserProfile;

public sealed record UserProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Bio,
    string? AvatarUrl);
