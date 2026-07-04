namespace Learnix.Infrastructure.Email.Models;

public sealed class UserUnbannedModel : BaseEmailModel
{
    public required string FirstName { get; init; }
}
