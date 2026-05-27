using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

public sealed class UserBannedModel
{
    public required string FirstName { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
