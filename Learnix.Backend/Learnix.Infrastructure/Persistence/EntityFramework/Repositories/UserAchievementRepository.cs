using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class UserAchievementRepository(ApplicationDbContext context)
    : RepositoryBase<UserAchievement>(context), IUserAchievementRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<bool> HasAchievementAsync(Guid userId, string code, CancellationToken ct)
        => _context.UserAchievements.AnyAsync(ua => ua.UserId == userId && ua.Code == code, ct);
}
