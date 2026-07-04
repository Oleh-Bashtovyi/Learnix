namespace Learnix.Infrastructure.Email.Models;

public sealed class InstructorApprovedModel : BaseEmailModel
{
    public required string FirstName { get; init; }
}
