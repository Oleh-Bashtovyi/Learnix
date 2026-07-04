namespace Learnix.Application.Users.Queries.GetMyProfile;

public sealed record MyProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Bio,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    bool HasPassword);
