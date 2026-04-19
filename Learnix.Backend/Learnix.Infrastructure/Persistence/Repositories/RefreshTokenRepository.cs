using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Specifications;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FirstOrDefaultAsync(Specification<RefreshToken> spec, CancellationToken ct = default)
        => SpecificationEvaluator<RefreshToken>.GetQuery(context.RefreshTokens, spec).FirstOrDefaultAsync(ct);

    public Task<List<RefreshToken>> ListAsync(Specification<RefreshToken> spec, CancellationToken ct = default)
        => SpecificationEvaluator<RefreshToken>.GetQuery(context.RefreshTokens, spec).ToListAsync(ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
        => await context.RefreshTokens.AddAsync(refreshToken, ct);
}