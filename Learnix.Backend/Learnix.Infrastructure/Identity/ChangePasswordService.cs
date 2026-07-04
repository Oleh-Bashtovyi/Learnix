using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Learnix.Infrastructure.Identity;

internal sealed class ChangePasswordService(UserManager<User> userManager) : IChangePasswordService
{
    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail(new NotFoundError("User not found."));

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
            return Result.Ok();

        // Specific mapping for bad current password
        if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
        {
            return Result.Fail(new Error("Incorrect current password."));
        }

        return Result.Fail(result.Errors.Select(e => (IError)new Error(e.Description)).ToList());
    }
}
