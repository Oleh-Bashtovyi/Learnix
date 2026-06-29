using FluentResults;

namespace Learnix.Application.Auth.Abstractions;

public interface IChangePasswordService
{
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);
}
