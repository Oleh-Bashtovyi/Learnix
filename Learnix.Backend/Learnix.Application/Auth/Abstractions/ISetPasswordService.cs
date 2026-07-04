using FluentResults;

namespace Learnix.Application.Auth.Abstractions;

public interface ISetPasswordService
{
    Task<Result> SetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);
}
