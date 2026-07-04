using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Auth.Specifications;

public sealed class RefreshTokenByHashSpecification : Specification<RefreshToken>, ISingleResultSpecification<RefreshToken>
{
    public RefreshTokenByHashSpecification(string tokenHash)
    {
        Query.Where(x => x.TokenHash == tokenHash);
    }
}
