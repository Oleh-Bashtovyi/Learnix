namespace Learnix.Infrastructure.Email.Models;

public sealed class UserBannedModel : BaseEmailModel
{
    public required string FirstName { get; init; }
}
