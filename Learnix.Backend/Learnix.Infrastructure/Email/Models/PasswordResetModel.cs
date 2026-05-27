using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

public sealed class PasswordResetModel
{
    public required string FirstName { get; init; }
    public required string ResetLink { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
