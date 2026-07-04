using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Learnix.Infrastructure.Identity;

internal sealed class SetPasswordService(UserManager<User> userManager) : ISetPasswordService
{
    public async Task<Result> SetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail(new NotFoundError("User not found."));

        if (user.PasswordHash != null)
            return Result.Fail(new Error("Password already set. Use Change Password instead."));

        var result = await userManager.AddPasswordAsync(user, newPassword);

        if (result.Succeeded)
            return Result.Ok();

        return Result.Fail(result.Errors.Select(e => (IError)new Error(e.Description)).ToList());
    }
}
