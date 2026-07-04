using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Auth.Specifications;
using Learnix.Application.Common.Abstractions.Persistence;
using MediatR;

namespace Learnix.Application.Auth.Commands.Logout;

internal sealed class LogoutCommandHandler(
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: missing or unknown token is not an error.
        if (string.IsNullOrEmpty(request.RefreshToken))
            return Result.Ok();

        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var token = await refreshTokenRepository.FirstOrDefaultAsync(
            new RefreshTokenByHashSpecification(hash), cancellationToken);

        if (token is not null && !token.IsRevoked)
        {
            token.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}
