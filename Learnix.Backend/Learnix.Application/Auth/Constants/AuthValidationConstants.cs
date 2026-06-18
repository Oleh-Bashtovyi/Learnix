namespace Learnix.Application.Auth.Constants;

public static class AuthValidationConstants
{
    /// <summary>RFC 5321 — max email length.</summary>
    public const int EmailMaxLength = 256;

    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public const int EmailConfirmationTokenExpirationMinutes = 3;
}
