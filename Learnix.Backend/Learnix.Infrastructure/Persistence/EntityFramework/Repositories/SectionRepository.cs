using Learnix.Application.Sections.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class SectionRepository(ApplicationDbContext context) : ISectionRepository
{
    public async Task BulkSetDisplayOrderAsync(IReadOnlyList<(Guid Id, int Order)> pairs, CancellationToken ct)
    {
        foreach (var (id, order) in pairs)
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Sections\" SET \"DisplayOrder\" = {order} WHERE \"Id\" = {id}", ct);
    }
}
