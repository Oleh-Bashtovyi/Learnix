using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Models;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Identity;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-006: Decomposition of Identity service into three roles based on SRP
/// </remarks>
internal sealed class UserRegistrationService(
    UserManager<User> userManager,
    IUnitOfWork unitOfWork)
    : IUserRegistrationService
{
    public async Task<Result<(Guid UserId, string EmailConfirmationToken)>> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string language = "en",
        CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Result.Fail<(Guid, string)>(new ConflictError("User with this email already exists."));

        var user = new User(email, firstName, lastName);
        user.SetLanguage(language);
        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
            return Result.Fail<(Guid, string)>(createResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Student);
        if (!roleResult.Succeeded)
            return Result.Fail<(Guid, string)>(roleResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        user.RaiseUserRegistered(token);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok((user.Id, token));
    }

    public async Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail(new NotFoundError("User not found."));

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Errors.Select(e => (IError)new Error(e.Description)).ToList());
    }

    public async Task<Result> ResendConfirmationEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed)
            return Result.Ok();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        user.RaiseUserRegistered(token);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<Guid>> FindOrCreateGoogleUserAsync(
        GoogleUserInfo googleUser,
        CancellationToken ct = default)
    {
        // Case 1: user with this GoogleId already exists → standard Google login
        var byGoogleId = await userManager.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.GoogleId, ct);

        if (byGoogleId is not null)
            return Result.Ok(byGoogleId.Id);

        // Case 2 & 2a: user with this email exists but no GoogleId linked yet
        var byEmail = await userManager.FindByEmailAsync(googleUser.Email);
        if (byEmail is not null)
        {
            if (byEmail.EmailConfirmed)
            {
                // Confirmed account → simple linking
                byEmail.SetGoogleId(googleUser.GoogleId);
            }
            else
            {
                // Unconfirmed account → takeover.
                // Google has verified this email, so the real owner gets the account.
                // Any password set by a previous (possibly malicious) registration is wiped.
                byEmail.ClaimViaGoogle(googleUser.GoogleId);
            }

            var updateResult = await userManager.UpdateAsync(byEmail);
            if (!updateResult.Succeeded)
                return Result.Fail<Guid>(
                    updateResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

            return Result.Ok(byEmail.Id);
        }

        // Case 3: no match — create new user
        var firstName = !string.IsNullOrWhiteSpace(googleUser.FirstName)
            ? googleUser.FirstName
            : ExtractFirstName(googleUser.Email);

        var lastName = !string.IsNullOrWhiteSpace(googleUser.LastName)
            ? googleUser.LastName
            : "User";

        var newUser = new User(googleUser.Email, firstName, lastName);
        newUser.SetGoogleId(googleUser.GoogleId);

        // CreateAsync without password → PasswordHash stays null (Google-only account)
        var createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
            return Result.Fail<Guid>(
                createResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

        // Google has verified the email, so confirm it directly.
        // UpdateAsync persists EmailConfirmed = true.
        newUser.ConfirmEmailFromGoogle();

        var confirmResult = await userManager.UpdateAsync(newUser);
        if (!confirmResult.Succeeded)
            return Result.Fail<Guid>(
                confirmResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

        var roleResult = await userManager.AddToRoleAsync(newUser, Roles.Student);
        if (!roleResult.Succeeded)
            return Result.Fail<Guid>(
                roleResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

        return Result.Ok(newUser.Id);
    }

    /// <summary>Fallback when Google doesn't provide given_name — take the local part of the email.</summary>
    private static string ExtractFirstName(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "User";

        var localPart = email[..atIndex];
        return char.ToUpperInvariant(localPart[0]) + localPart[1..];
    }
}
