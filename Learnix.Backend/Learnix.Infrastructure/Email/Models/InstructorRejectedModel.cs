using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

public sealed class InstructorRejectedModel
{
    public required string FirstName { get; init; }
    public string? RejectionReason { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
