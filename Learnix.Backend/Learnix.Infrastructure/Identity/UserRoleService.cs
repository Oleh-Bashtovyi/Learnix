using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Identity;

internal sealed class UserRoleService(
    UserManager<User> userManager,
    ApplicationDbContext db) : IUserRoleService
{
    public async Task AssignRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await FindUserIgnoringFiltersAsync(userId, ct);
        if (user is null) return;

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }

    public async Task RemoveRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await FindUserIgnoringFiltersAsync(userId, ct);
        if (user is null) return;

        if (await userManager.IsInRoleAsync(user, role))
            await userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task<IList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await FindUserIgnoringFiltersAsync(userId, ct);
        if (user is null) return [];

        return await userManager.GetRolesAsync(user);
    }

    public async Task<int> CountUsersInRoleAsync(string role, CancellationToken ct = default)
    {
        var users = await userManager.GetUsersInRoleAsync(role);
        return users.Count;
    }

    // Uses IgnoreQueryFilters so it works for soft-deleted users too (e.g. admin recovery flows).
    private Task<User?> FindUserIgnoringFiltersAsync(Guid userId, CancellationToken ct)
        => db.Set<User>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
}
