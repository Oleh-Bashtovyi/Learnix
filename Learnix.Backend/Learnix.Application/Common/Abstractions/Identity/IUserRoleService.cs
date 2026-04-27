namespace Learnix.Application.Common.Abstractions.Identity;

public interface IUserRoleService
{
    Task AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
}
