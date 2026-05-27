using Microsoft.Extensions.Localization;

namespace Learnix.Infrastructure.Email.Models;

public sealed class CourseAdminActionModel
{
    public required string InstructorFirstName { get; init; }
    public required string CourseTitle { get; init; }
    public required IStringLocalizer Strings { get; init; }
}
