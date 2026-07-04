namespace Learnix.Application.Users.Queries.GetAdminUsers;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    IReadOnlyList<string> Roles,
    bool IsBanned,
    bool IsDeleted,
    DateTime CreatedAt
);
