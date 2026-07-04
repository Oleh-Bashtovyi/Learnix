using System.Text;
using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Learnix.Infrastructure.Identity;

internal sealed class PasswordResetService(
    UserManager<User> userManager,
    IUnitOfWork unitOfWork)
    : IPasswordResetService
{
    public async Task<Result> InitiateResetAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Anti-enumeration: same Ok response regardless of whether user exists.
        // If email is not confirmed — user can't log in anyway; sending reset link is pointless
        // and would leak that the email is registered.
        if (user is null || !user.EmailConfirmed)
            return Result.Ok();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        user.RaisePasswordResetRequested(token);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken ct = default)
    {
        // By this point the user already knows the email is registered (they got the reset email).
        // Anti-enumeration is not a concern here — return real errors for better UX.
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Fail(new NotFoundError("User not found."));

        // Reverse the base64-url encoding that was applied when building the reset link
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        var result = await userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (result.Succeeded)
            return Result.Ok();

        // Identity returns: InvalidToken, PasswordRequiresDigit, PasswordTooShort, etc.
        // Map as generic errors → fallthrough in ToActionResult → 400.
        return Result.Fail(result.Errors.Select(e => (IError)new Error(e.Description)).ToList());
    }
}
