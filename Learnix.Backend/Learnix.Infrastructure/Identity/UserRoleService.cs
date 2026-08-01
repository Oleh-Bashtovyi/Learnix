using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Identity;

internal sealed class UserRoleService(
    UserManager<User> userManager,
    ApplicationDbContext db) : IUserRoleService
{
    public async Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await FindUserIgnoringFiltersAsync(userId, cancellationToken);
        if (user is null) return;

        if (await userManager.IsInRoleAsync(user, role))
            return;

        var result = await userManager.AddToRoleAsync(user, role);
        ThrowIfFailed(result, $"assign role '{role}' to user {userId}");
    }

    public async Task RemoveRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await FindUserIgnoringFiltersAsync(userId, cancellationToken);
        if (user is null) return;

        if (!await userManager.IsInRoleAsync(user, role))
            return;

        var result = await userManager.RemoveFromRoleAsync(user, role);
        ThrowIfFailed(result, $"remove role '{role}' from user {userId}");
    }

    // AddToRoleAsync/RemoveFromRoleAsync return a failed IdentityResult instead of throwing (e.g. on a
    // concurrency-stamp conflict) — silently discarding it would let a caller believe the role change
    // succeeded when it did not.
    private static void ThrowIfFailed(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }

    public async Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserIgnoringFiltersAsync(userId, cancellationToken);
        if (user is null) return [];

        return await userManager.GetRolesAsync(user);
    }

    public async Task<Dictionary<Guid, IReadOnlyList<string>>> GetRolesBulkAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();

        var pairs = await db.Set<IdentityUserRole<Guid>>()
            .Where(ur => ids.Contains(ur.UserId))
            .Join(
                db.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => (IReadOnlyList<string>)pairs
                .Where(p => p.UserId == id)
                .Select(p => p.Name!)
                .ToList());
    }

    public async Task<int> CountUsersInRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var users = await userManager.GetUsersInRoleAsync(role);
        return users.Count;
    }

    // Uses IgnoreQueryFilters so it works for soft-deleted users too (e.g. admin recovery flows).
    private Task<User?> FindUserIgnoringFiltersAsync(Guid userId, CancellationToken cancellationToken)
        => db.Set<User>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
}
