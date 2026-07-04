using Learnix.Application.Common.Models;

namespace Learnix.Application.Auth.Abstractions;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        IReadOnlyList<string> roles,
        bool emailConfirmed);

    RefreshTokenResult GenerateRefreshToken();
    string HashRefreshToken(string plainToken);
}
