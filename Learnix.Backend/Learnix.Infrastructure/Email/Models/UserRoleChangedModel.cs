namespace Learnix.Infrastructure.Email.Models;

public sealed class UserRoleChangedModel : BaseEmailModel
{
    public required string FirstName { get; init; }
    public required string Role { get; init; }
    public required bool Assigned { get; init; }
}
