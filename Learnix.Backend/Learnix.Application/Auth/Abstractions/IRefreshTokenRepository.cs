using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Auth.Abstractions;

public interface IRefreshTokenRepository : IRepositoryBase<RefreshToken>
{
}
