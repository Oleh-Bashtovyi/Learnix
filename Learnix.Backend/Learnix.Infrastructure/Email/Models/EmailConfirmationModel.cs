namespace Learnix.Infrastructure.Email.Models;

public sealed class EmailConfirmationModel : BaseEmailModel
{
    public required string FirstName { get; init; }
    public required string ConfirmationCode { get; init; }
}
