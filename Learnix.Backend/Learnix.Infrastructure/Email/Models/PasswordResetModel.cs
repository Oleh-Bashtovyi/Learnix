namespace Learnix.Infrastructure.Email.Models;

public sealed class PasswordResetModel : BaseEmailModel
{
    public required string FirstName { get; init; }
    public required string ResetLink { get; init; }
}
