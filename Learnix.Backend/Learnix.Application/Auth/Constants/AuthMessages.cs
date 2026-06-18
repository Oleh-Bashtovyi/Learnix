namespace Learnix.Application.Auth.Constants;

public static class AuthMessages
{
    public static string UserWithEmailExists => "User with this email already exists.";
    public static string EmailNotConfirmed => "Email is not confirmed.";
    public static string EmailAlreadyConfirmed => "Email is already confirmed.";
    public static string InvalidConfirmationCode => "Invalid confirmation code.";
    public static string InvalidRefreshToken => "Invalid refresh token.";
    public static string RefreshTokenExpired => "Refresh token expired.";
}
