using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

public abstract class BaseEmailModel
{
    public required string ClientBaseUrl { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
