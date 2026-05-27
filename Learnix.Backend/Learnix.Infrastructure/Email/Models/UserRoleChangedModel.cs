using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

public sealed class UserRoleChangedModel
{
    public required string FirstName { get; init; }
    public required string Role { get; init; }
    public required bool Assigned { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
