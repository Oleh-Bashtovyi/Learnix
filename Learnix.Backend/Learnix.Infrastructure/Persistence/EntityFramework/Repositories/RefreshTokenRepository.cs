using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Auth.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class RefreshTokenRepository(ApplicationDbContext context)
    : RepositoryBase<RefreshToken>(context), IRefreshTokenRepository
{
}
