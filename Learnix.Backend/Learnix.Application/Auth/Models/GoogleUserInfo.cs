namespace Learnix.Application.Auth.Models;

public sealed record GoogleUserInfo(
    string GoogleId,
    string Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName);
