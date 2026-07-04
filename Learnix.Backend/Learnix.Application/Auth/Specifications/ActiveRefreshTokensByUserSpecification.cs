using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Auth.Specifications;

public sealed class ActiveRefreshTokensByUserSpecification : Specification<RefreshToken>
{
    public ActiveRefreshTokensByUserSpecification(Guid userId)
    {
        var now = DateTime.UtcNow;

        Query.Where(x => x.UserId == userId && !x.IsRevoked && x.ExpiresAt > now);
    }
}
