using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Models;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Learnix.Infrastructure.Identity;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-006: Decomposition of Identity service into three roles based on SRP
/// </remarks>
internal sealed class UserAuthenticationService(
    UserManager<User> userManager,
    SignInManager<User> signInManager)
    : IUserAuthenticationService
{
    public async Task<Result<UserAuthenticationInfo>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Fail<UserAuthenticationInfo>(new AuthenticationError("Invalid credentials."));

        // lockoutOnFailure: true — SignInManager tracks failed attempts and applies lockout
        var signIn = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signIn.IsLockedOut)
            return Result.Fail<UserAuthenticationInfo>(
                new AuthenticationError("Account is locked. Please try again later."));

        if (!signIn.Succeeded)
            return Result.Fail<UserAuthenticationInfo>(new AuthenticationError("Invalid credentials."));

        var roles = await userManager.GetRolesAsync(user);
        return Result.Ok(new UserAuthenticationInfo(
            user.Id, user.Email!, user.FirstName, user.LastName,
            roles.ToList().AsReadOnly(), user.EmailConfirmed, user.AvatarBlobPath));
    }

    public async Task<Result<UserAuthenticationInfo>> GetAuthenticationInfoAsync(
        Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail<UserAuthenticationInfo>(new NotFoundError("User not found."));

        var roles = await userManager.GetRolesAsync(user);
        return Result.Ok(new UserAuthenticationInfo(
            user.Id, user.Email!, user.FirstName, user.LastName,
            roles.ToList().AsReadOnly(), user.EmailConfirmed, user.AvatarBlobPath));
    }
}
