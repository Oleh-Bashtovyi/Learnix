namespace Learnix.Infrastructure.Email.Models;

public sealed class CourseAdminActionModel : BaseEmailModel
{
    public required string InstructorFirstName { get; init; }
    public required string CourseTitle { get; init; }
}
