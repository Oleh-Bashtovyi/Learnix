namespace Learnix.Infrastructure.Email.Models;

public sealed class InstructorRejectedModel : BaseEmailModel
{
    public required string FirstName { get; init; }
    public string? RejectionReason { get; init; }
}
