namespace Learnix.Application.Common.Abstractions.Identity;

public interface IUserRoleService
{
    Task AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task RemoveRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);
    Task<Dictionary<Guid, IReadOnlyList<string>>> GetRolesBulkAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task<int> CountUsersInRoleAsync(string role, CancellationToken ct = default);
}
